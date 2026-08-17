use mq_proto::raft_rpc_server::RaftRpc;
use mq_proto::{RaftEnvelope, RaftGroup};
use mq_raft::{NodeRaft, QueueRaft};
use tonic::{Request, Response, Status};

/// Multiplexes the single `RaftRpc` gRPC service across this node's two
/// independent `openraft::Raft` instances, dispatching each envelope by
/// its `group` tag (see proto/mq.proto) to the "nodes" or "spam" group.
pub struct RaftRpcImpl {
    pub node_raft: NodeRaft,
    pub queue_raft: QueueRaft,
}

fn to_status<E: std::fmt::Debug>(err: E) -> Status {
    Status::internal(format!("{err:?}"))
}

fn group_of(envelope: &RaftEnvelope) -> RaftGroup {
    RaftGroup::try_from(envelope.group).unwrap_or(RaftGroup::Spam)
}

#[tonic::async_trait]
impl RaftRpc for RaftRpcImpl {
    async fn append_entries(
        &self,
        request: Request<RaftEnvelope>,
    ) -> Result<Response<RaftEnvelope>, Status> {
        let envelope = request.into_inner();
        let payload = match group_of(&envelope) {
            RaftGroup::Nodes => {
                let req = serde_json::from_slice(&envelope.payload).map_err(to_status)?;
                let resp = self
                    .node_raft
                    .append_entries(req)
                    .await
                    .map_err(to_status)?;
                serde_json::to_vec(&resp).map_err(to_status)?
            }
            RaftGroup::Spam => {
                let req = serde_json::from_slice(&envelope.payload).map_err(to_status)?;
                let resp = self
                    .queue_raft
                    .append_entries(req)
                    .await
                    .map_err(to_status)?;
                serde_json::to_vec(&resp).map_err(to_status)?
            }
        };
        Ok(Response::new(RaftEnvelope {
            payload,
            group: envelope.group,
        }))
    }

    async fn vote(&self, request: Request<RaftEnvelope>) -> Result<Response<RaftEnvelope>, Status> {
        let envelope = request.into_inner();
        let payload = match group_of(&envelope) {
            RaftGroup::Nodes => {
                let req = serde_json::from_slice(&envelope.payload).map_err(to_status)?;
                let resp = self.node_raft.vote(req).await.map_err(to_status)?;
                serde_json::to_vec(&resp).map_err(to_status)?
            }
            RaftGroup::Spam => {
                let req = serde_json::from_slice(&envelope.payload).map_err(to_status)?;
                let resp = self.queue_raft.vote(req).await.map_err(to_status)?;
                serde_json::to_vec(&resp).map_err(to_status)?
            }
        };
        Ok(Response::new(RaftEnvelope {
            payload,
            group: envelope.group,
        }))
    }

    async fn install_snapshot(
        &self,
        request: Request<RaftEnvelope>,
    ) -> Result<Response<RaftEnvelope>, Status> {
        let envelope = request.into_inner();
        let payload = match group_of(&envelope) {
            RaftGroup::Nodes => {
                let req = serde_json::from_slice(&envelope.payload).map_err(to_status)?;
                let resp = self
                    .node_raft
                    .install_snapshot(req)
                    .await
                    .map_err(to_status)?;
                serde_json::to_vec(&resp).map_err(to_status)?
            }
            RaftGroup::Spam => {
                let req = serde_json::from_slice(&envelope.payload).map_err(to_status)?;
                let resp = self
                    .queue_raft
                    .install_snapshot(req)
                    .await
                    .map_err(to_status)?;
                serde_json::to_vec(&resp).map_err(to_status)?
            }
        };
        Ok(Response::new(RaftEnvelope {
            payload,
            group: envelope.group,
        }))
    }
}
