use std::collections::{BTreeMap, HashMap};
use std::sync::{Arc, Mutex};
use std::time::Duration;

use futures::Stream;
use openraft::error::{ClientWriteError, RaftError};
use std::pin::Pin;
use tokio_stream::StreamExt;
use tonic::transport::Channel;
use tonic::{Request, Response, Status, Streaming};

use mq_proto::mq_service_client::MqServiceClient;
use mq_proto::{
    ConsumeRequest, ConsumeResponse, PublishRequest, PublishResponse, StatusRequest, StatusUpdate,
    Subscribe, consume_request::Kind, mq_service_server::MqService,
};
use mq_raft::{QueueRequest, QueueState};

pub struct MqServiceImpl {
    pub node_id: String,
    pub raft: mq_raft::QueueRaft,
    pub node_raft: mq_raft::NodeRaft,
    pub state: Arc<std::sync::Mutex<QueueState>>,
    /// Node id -> gRPC address, used to forward writes to whoever is
    /// currently the "spam" partition's Raft leader.
    pub peers: Arc<BTreeMap<u64, String>>,
    /// Lazily-connected, reused channels to peers - forwarding used to
    /// dial a brand new connection per message (see mq-raft's Network,
    /// same bug), which capped follower-forwarded throughput at ~60
    /// msg/s. Caching the channel here fixes that the same way.
    pub forward_channels: Arc<Mutex<HashMap<u64, Channel>>>,
}

impl MqServiceImpl {
    fn channel_for(&self, id: u64, addr: &str) -> Result<Channel, Status> {
        let mut channels = self.forward_channels.lock().unwrap();
        if let Some(ch) = channels.get(&id) {
            return Ok(ch.clone());
        }
        let ch = Channel::from_shared(addr.to_string())
            .map_err(|e| Status::internal(e.to_string()))?
            .connect_lazy();
        channels.insert(id, ch.clone());
        Ok(ch)
    }

    /// Proposes `req` through Raft, transparently forwarding to the
    /// current leader (by re-issuing the same app-level RPC there) if
    /// this node isn't it. This is what lets the producer/consumer
    /// always connect to a fixed node regardless of which of the 3
    /// nodes actually holds leadership.
    async fn propose(&self, req: QueueRequest) -> Result<mq_raft::QueueResponse, Status> {
        match self.raft.client_write(req.clone()).await {
            Ok(resp) => Ok(resp.data),
            Err(RaftError::APIError(ClientWriteError::ForwardToLeader(fwd))) => {
                let Some(leader_id) = fwd.leader_id else {
                    return Err(Status::unavailable("no raft leader elected yet"));
                };
                let addr = self
                    .peers
                    .get(&leader_id)
                    .ok_or_else(|| Status::internal("unknown leader address"))?
                    .clone();
                self.forward(leader_id, &addr, req).await
            }
            Err(err) => Err(Status::internal(format!("raft write failed: {err:?}"))),
        }
    }

    async fn forward(
        &self,
        leader_id: u64,
        addr: &str,
        req: QueueRequest,
    ) -> Result<mq_raft::QueueResponse, Status> {
        let channel = self.channel_for(leader_id, addr)?;
        let mut client = MqServiceClient::new(channel);
        match req {
            QueueRequest::Publish { queue, payload } => {
                let resp = client
                    .publish(PublishRequest { queue, payload })
                    .await?
                    .into_inner();
                Ok(mq_raft::QueueResponse {
                    published_offset: Some(resp.offset),
                    popped: None,
                })
            }
            QueueRequest::Pop { .. } => {
                // Pop forwarding isn't needed: Consume already loops
                // locally and will keep proposing until it lands on the
                // leader via this same forwarding path per-attempt.
                Err(Status::unavailable("retry against leader"))
            }
        }
    }
}

#[tonic::async_trait]
impl MqService for MqServiceImpl {
    async fn publish(
        &self,
        request: Request<PublishRequest>,
    ) -> Result<Response<PublishResponse>, Status> {
        let req = request.into_inner();
        let resp = self
            .propose(QueueRequest::Publish {
                queue: req.queue,
                payload: req.payload,
            })
            .await?;
        Ok(Response::new(PublishResponse {
            offset: resp.published_offset.unwrap_or_default(),
        }))
    }

    type ConsumeStream = Pin<Box<dyn Stream<Item = Result<ConsumeResponse, Status>> + Send>>;

    async fn consume(
        &self,
        request: Request<Streaming<ConsumeRequest>>,
    ) -> Result<Response<Self::ConsumeStream>, Status> {
        let mut inbound = request.into_inner();

        let first = inbound
            .next()
            .await
            .ok_or_else(|| Status::invalid_argument("expected Subscribe as first message"))??;
        let Some(Kind::Subscribe(Subscribe { queue: queue_name })) = first.kind else {
            return Err(Status::invalid_argument("first message must be Subscribe"));
        };

        // Acks are auto-applied on pop (see QueueRequest::Pop); drain the
        // inbound stream so the client's Ack sends don't error out.
        tokio::spawn(async move { while inbound.next().await.is_some() {} });

        let raft = self.raft.clone();
        let peers = self.peers.clone();
        let forward_channels = self.forward_channels.clone();

        let outbound = async_stream::stream! {
            loop {
                let propose_result = raft.client_write(QueueRequest::Pop { queue: queue_name.clone() }).await;
                let popped = match propose_result {
                    Ok(resp) => resp.data.popped,
                    Err(RaftError::APIError(ClientWriteError::ForwardToLeader(fwd))) => {
                        match fwd.leader_id.and_then(|id| peers.get(&id).map(|addr| (id, addr.clone()))) {
                            Some((id, addr)) => match pop_via_forward(&forward_channels, id, &addr, &queue_name).await {
                                Ok(popped) => popped,
                                Err(_) => { tokio::time::sleep(Duration::from_millis(200)).await; continue; }
                            },
                            None => { tokio::time::sleep(Duration::from_millis(200)).await; continue; }
                        }
                    }
                    Err(_) => { tokio::time::sleep(Duration::from_millis(200)).await; continue; }
                };

                match popped {
                    Some((offset, payload)) => yield Ok(ConsumeResponse { offset, payload }),
                    None => tokio::time::sleep(Duration::from_millis(150)).await,
                }
            }
        };

        Ok(Response::new(Box::pin(outbound)))
    }

    type WatchStatusStream = Pin<Box<dyn Stream<Item = Result<StatusUpdate, Status>> + Send>>;

    async fn watch_status(
        &self,
        _request: Request<StatusRequest>,
    ) -> Result<Response<Self::WatchStatusStream>, Status> {
        let state = self.state.clone();
        let node_id = self.node_id.clone();

        let node_raft = self.node_raft.clone();
        let queue_raft = self.raft.clone();

        let outbound = async_stream::stream! {
            let mut interval = tokio::time::interval(Duration::from_millis(500));
            loop {
                interval.tick().await;
                let queue_depths = state.lock().unwrap().depths();
                let nodes_group_leader = node_raft.current_leader().await.unwrap_or_default();
                let queue_group_leader = queue_raft.current_leader().await.unwrap_or_default();
                yield Ok(StatusUpdate {
                    node_id: node_id.clone(),
                    queue_depths,
                    nodes_group_leader,
                    queue_group_leader,
                });
            }
        };

        Ok(Response::new(Box::pin(outbound)))
    }
}

async fn pop_via_forward(
    channels: &Mutex<HashMap<u64, Channel>>,
    leader_id: u64,
    addr: &str,
    queue: &str,
) -> Result<Option<(u64, Vec<u8>)>, Status> {
    let channel = {
        let mut channels = channels.lock().unwrap();
        if let Some(ch) = channels.get(&leader_id) {
            ch.clone()
        } else {
            let ch = Channel::from_shared(addr.to_string())
                .map_err(|e| Status::internal(e.to_string()))?
                .connect_lazy();
            channels.insert(leader_id, ch.clone());
            ch
        }
    };
    let mut client = MqServiceClient::new(channel);
    let (tx, rx) = tokio::sync::mpsc::channel(4);
    tx.send(ConsumeRequest {
        kind: Some(Kind::Subscribe(Subscribe {
            queue: queue.to_string(),
        })),
    })
    .await
    .map_err(|_| Status::internal("forward channel closed"))?;
    let outbound = tokio_stream::wrappers::ReceiverStream::new(rx);
    let mut inbound = client.consume(outbound).await?.into_inner();
    match tokio::time::timeout(Duration::from_millis(300), inbound.next()).await {
        Ok(Some(Ok(resp))) => Ok(Some((resp.offset, resp.payload))),
        _ => Ok(None),
    }
}
