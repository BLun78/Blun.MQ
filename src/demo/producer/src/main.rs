use std::sync::atomic::{AtomicU64, Ordering};
use std::sync::Arc;
use std::time::Duration;

use mq_client::MqClient;

#[tokio::main]
async fn main() -> anyhow::Result<()> {
    tracing_subscriber::fmt().init();

    // In the Aspire scenario the producer connects to node 2.
    let addr = std::env::var("MQ_NODE_ADDR").unwrap_or_else(|_| "http://127.0.0.1:5002".into());
    let queue = std::env::var("MQ_QUEUE").unwrap_or_else(|_| "spam".into());
    // Target publish rate. Each publish is a separate gRPC round-trip, so
    // hitting ~1000/s means firing them concurrently rather than awaiting
    // each one before starting the next - the tokio::spawn below lets
    // publishes overlap on the same HTTP/2 connection instead of being
    // serialized behind round-trip latency.
    let rate_per_sec: u64 = std::env::var("MQ_RATE_PER_SEC")
        .ok()
        .and_then(|v| v.parse().ok())
        .unwrap_or(1000);

    tracing::info!(addr, queue, rate_per_sec, "producer connecting");
    // Aspire's WaitFor only guarantees the target process has started,
    // not that its gRPC server is already accepting connections (no
    // health check is wired up), so retry the initial connect.
    let client = loop {
        match MqClient::connect(addr.clone()).await {
            Ok(client) => break client,
            Err(err) => {
                tracing::warn!(%err, "connect failed, retrying");
                tokio::time::sleep(Duration::from_millis(500)).await;
            }
        }
    };

    let published = Arc::new(AtomicU64::new(0));
    let failed = Arc::new(AtomicU64::new(0));

    // Log a throughput summary once a second instead of per-message, which
    // would otherwise dominate cost at this rate.
    {
        let published = published.clone();
        let failed = failed.clone();
        tokio::spawn(async move {
            let mut interval = tokio::time::interval(Duration::from_secs(1));
            loop {
                interval.tick().await;
                let published = published.swap(0, Ordering::Relaxed);
                let failed = failed.swap(0, Ordering::Relaxed);
                tracing::info!(published, failed, "publish rate (last 1s)");
            }
        });
    }

    let mut ticker = tokio::time::interval(Duration::from_micros(1_000_000 / rate_per_sec.max(1)));
    let mut counter: u64 = 0;
    loop {
        ticker.tick().await;
        counter += 1;
        let mut client = client.clone();
        let queue = queue.clone();
        let published = published.clone();
        let failed = failed.clone();
        tokio::spawn(async move {
            let payload = format!("spam message #{counter}").into_bytes();
            match client.publish(&queue, payload).await {
                Ok(_offset) => {
                    published.fetch_add(1, Ordering::Relaxed);
                }
                Err(err) => {
                    failed.fetch_add(1, Ordering::Relaxed);
                    tracing::debug!(%err, counter, "publish failed");
                }
            }
        });
    }
}
