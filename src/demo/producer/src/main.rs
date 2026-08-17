use std::time::Duration;

use mq_client::MqClient;

#[tokio::main]
async fn main() -> anyhow::Result<()> {
    tracing_subscriber::fmt().init();

    // In the Aspire scenario the producer connects to node 2.
    let addr = std::env::var("MQ_NODE_ADDR").unwrap_or_else(|_| "http://127.0.0.1:5002".into());
    let queue = std::env::var("MQ_QUEUE").unwrap_or_else(|_| "spam".into());

    tracing::info!(addr, queue, "producer connecting");
    // Aspire's WaitFor only guarantees the target process has started,
    // not that its gRPC server is already accepting connections (no
    // health check is wired up), so retry the initial connect.
    let mut client = loop {
        match MqClient::connect(addr.clone()).await {
            Ok(client) => break client,
            Err(err) => {
                tracing::warn!(%err, "connect failed, retrying");
                tokio::time::sleep(Duration::from_millis(500)).await;
            }
        }
    };

    let mut ticker = tokio::time::interval(Duration::from_secs(1));
    let mut counter: u64 = 0;
    loop {
        ticker.tick().await;
        counter += 1;
        let payload = format!("spam message #{counter}").into_bytes();
        match client.publish(&queue, payload).await {
            Ok(offset) => tracing::info!(counter, offset, "published"),
            Err(err) => tracing::warn!(%err, counter, "publish failed, will retry next tick"),
        }
    }
}
