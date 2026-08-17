use mq_proto::mq_service_client::MqServiceClient;
use mq_proto::{consume_request::Kind, ConsumeRequest, PublishRequest, Subscribe};
use tokio::sync::mpsc;
use tokio_stream::wrappers::ReceiverStream;
use tokio_stream::StreamExt;
use tonic::transport::Channel;

/// Distinguishes "couldn't reach the node" from "the node rejected the
/// call" - callers (retry loops in the demos, load tests) generally want
/// to react differently to the two (backoff-and-retry-forever vs.
/// surfacing an application error).
#[derive(Debug, thiserror::Error)]
pub enum MqClientError {
    #[error("invalid address: {0}")]
    InvalidAddress(#[from] tonic::codegen::http::uri::InvalidUri),
    #[error("rpc failed: {0}")]
    Rpc(#[from] tonic::Status),
    #[error("outbound channel closed before request could be sent")]
    ChannelClosed,
}

pub type Result<T> = std::result::Result<T, MqClientError>;

#[derive(Clone)]
pub struct MqClient {
    inner: MqServiceClient<Channel>,
}

impl MqClient {
    /// Builds a client without blocking on a connection attempt - the
    /// underlying HTTP/2 connection is established lazily on the first
    /// call and reused after that. Connecting eagerly here would repeat
    /// the same "reconnect per call" mistake fixed elsewhere in this
    /// codebase (see mq-raft's Network and mq-node's forward()) for
    /// anyone who calls `connect` in a hot loop instead of caching the
    /// client.
    pub fn connect(addr: impl Into<String>) -> Result<Self> {
        let channel = Channel::from_shared(addr.into())?.connect_lazy();
        Ok(Self {
            inner: MqServiceClient::new(channel),
        })
    }

    pub async fn publish(&mut self, queue: &str, payload: Vec<u8>) -> Result<u64> {
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
    ///
    /// Note: the wire protocol has an `Ack` message for client-driven
    /// at-least-once redelivery, but the server currently auto-acks each
    /// message the moment it's popped (see mq-node's `QueueRequest::Pop`)
    /// - so this is effectively at-most-once today, and there is no way
    /// for a caller of this method to send an `Ack`. Wiring up real
    /// redelivery (server holds the message until acked, redelivers on
    /// timeout) is a follow-up, not implemented here.
    pub async fn consume(
        &mut self,
        queue: &str,
    ) -> Result<impl tokio_stream::Stream<Item = (u64, Vec<u8>)>> {
        let (tx, rx) = mpsc::channel(16);
        tx.send(ConsumeRequest {
            kind: Some(Kind::Subscribe(Subscribe {
                queue: queue.to_string(),
            })),
        })
        .await
        .map_err(|_| MqClientError::ChannelClosed)?;

        let outbound = ReceiverStream::new(rx);
        let response = self.inner.consume(outbound).await?;
        let inbound = response.into_inner();

        Ok(inbound.filter_map(|item| item.ok().map(|r| (r.offset, r.payload))))
    }
}
