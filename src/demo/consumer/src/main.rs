use std::sync::atomic::{AtomicU64, Ordering};
use std::sync::Arc;
use std::time::Duration;

use mq_client::MqClient;
use tokio_stream::StreamExt;

#[tokio::main]
async fn main() -> anyhow::Result<()> {
    tracing_subscriber::fmt().init();

    // In the Aspire scenario the consumer connects to node 1.
    let addr = std::env::var("MQ_NODE_ADDR").unwrap_or_else(|_| "http://127.0.0.1:5001".into());
    let queue = std::env::var("MQ_QUEUE").unwrap_or_else(|_| "spam".into());
    // Each Consume RPC already runs several concurrent Pop-proposal
    // workers server-side (see mq-node's grpc.rs), but opening several
    // concurrent streams here too further parallelizes pop throughput,
    // the same way the producer's concurrency pool parallelizes
    // publishes - useful once a single client's own gRPC handling
    // becomes the bottleneck rather than the server's Raft round-trips.
    let concurrency: usize = std::env::var("MQ_CONSUMER_CONCURRENCY")
        .unwrap_or_else(|_| "8".into())
        .parse()?;

    tracing::info!(addr, queue, concurrency, "consumer starting");

    let received = Arc::new(AtomicU64::new(0));
    {
        let received = received.clone();
        tokio::spawn(async move {
            let mut interval = tokio::time::interval(Duration::from_secs(1));
            let mut last = 0u64;
            loop {
                interval.tick().await;
                let now = received.load(Ordering::Relaxed);
                tracing::info!(msgs_per_sec = now - last, total_received = now, "throughput");
                last = now;
            }
        });
    }

    let mut workers = Vec::new();
    for worker_id in 0..concurrency {
        let addr = addr.clone();
        let queue = queue.clone();
        let received = received.clone();
        workers.push(tokio::spawn(async move {
            loop {
                match run(&addr, &queue, &received).await {
                    Ok(()) => {}
                    Err(err) => tracing::warn!(worker_id, %err, "consume stream ended, reconnecting"),
                }
                tokio::time::sleep(Duration::from_millis(500)).await;
            }
        }));
    }
    for w in workers {
        w.await?;
    }

    Ok(())
}

async fn run(addr: &str, queue: &str, received: &AtomicU64) -> anyhow::Result<()> {
    let mut client = MqClient::connect(addr.to_string())?;
    let mut stream = Box::pin(client.consume(queue).await?);
    while let Some((offset, payload)) = stream.next().await {
        received.fetch_add(1, Ordering::Relaxed);
        tracing::debug!(offset, len = payload.len(), "received");
    }
    Ok(())
}
