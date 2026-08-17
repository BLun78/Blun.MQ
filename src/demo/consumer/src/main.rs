use std::time::Duration;

use mq_client::MqClient;
use tokio_stream::StreamExt;

#[tokio::main]
async fn main() -> anyhow::Result<()> {
    tracing_subscriber::fmt().init();

    // In the Aspire scenario the consumer connects to node 1.
    let addr = std::env::var("MQ_NODE_ADDR").unwrap_or_else(|_| "http://127.0.0.1:5001".into());
    let queue = std::env::var("MQ_QUEUE").unwrap_or_else(|_| "spam".into());

    tracing::info!(addr, queue, "consumer connecting");

    // Aspire's WaitFor only guarantees the target process has started,
    // not that its gRPC server is already accepting connections, and the
    // stream can also drop if the node we're on loses Raft leadership -
    // so this outer loop just keeps reconnecting.
    loop {
        match run(&addr, &queue).await {
            Ok(()) => {}
            Err(err) => tracing::warn!(%err, "consume stream ended, reconnecting"),
        }
        tokio::time::sleep(Duration::from_millis(500)).await;
    }
}

async fn run(addr: &str, queue: &str) -> anyhow::Result<()> {
    let mut client = MqClient::connect(addr.to_string()).await?;
    let mut stream = Box::pin(client.consume(queue).await?);
    while let Some((offset, payload)) = stream.next().await {
        let text = String::from_utf8_lossy(&payload);
        tracing::info!(offset, message = %text, "received");
    }
    Ok(())
}
