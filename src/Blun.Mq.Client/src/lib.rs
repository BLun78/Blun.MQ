use mq_proto::mq_service_client::MqServiceClient;
use mq_proto::{consume_request::Kind, ConsumeRequest, PublishRequest, Subscribe};
use tokio::sync::mpsc;
use tokio_stream::wrappers::ReceiverStream;
use tokio_stream::StreamExt;
use tonic::transport::Channel;

pub struct MqClient {
    inner: MqServiceClient<Channel>,
}

impl MqClient {
    pub async fn connect(addr: impl Into<String>) -> anyhow::Result<Self> {
        let inner = MqServiceClient::connect(addr.into()).await?;
        Ok(Self { inner })
    }

    pub async fn publish(&mut self, queue: &str, payload: Vec<u8>) -> anyhow::Result<u64> {
        let resp = self
            .inner
            .publish(PublishRequest {
                queue: queue.to_string(),
                payload,
            })
            .await?;
        Ok(resp.into_inner().offset)
    }

    /// Subscribes to a queue and returns a stream of (offset, payload).
    /// Acks are sent automatically on delivery (see mq-node's queue.rs).
    pub async fn consume(
        &mut self,
        queue: &str,
    ) -> anyhow::Result<impl tokio_stream::Stream<Item = (u64, Vec<u8>)>> {
        let (tx, rx) = mpsc::channel(16);
        tx.send(ConsumeRequest {
            kind: Some(Kind::Subscribe(Subscribe {
                queue: queue.to_string(),
            })),
        })
        .await?;

        let outbound = ReceiverStream::new(rx);
        let response = self.inner.consume(outbound).await?;
        let inbound = response.into_inner();

        Ok(inbound.filter_map(|item| item.ok().map(|r| (r.offset, r.payload))))
    }
}
