use std::sync::atomic::{AtomicU64, Ordering};
use std::sync::Arc;
use std::time::Duration;

use mq_client::MqClient;
use tokio::sync::Semaphore;

#[tokio::main]
async fn main() -> anyhow::Result<()> {
    tracing_subscriber::fmt().init();

    // In the Aspire scenario the producer connects to node 2.
    let addr = std::env::var("MQ_NODE_ADDR").unwrap_or_else(|_| "http://127.0.0.1:5002".into());
    let queue = std::env::var("MQ_QUEUE").unwrap_or_else(|_| "spam".into());
    let rate: u64 = std::env::var("MQ_RATE")
        .unwrap_or_else(|_| "1000".into())
        .parse()?;
    // Caps how many publishes can be in flight at once. At 1000 msg/s and
    // ~0.3-1ms/publish under load (see mq-bench numbers) a few hundred
    // in-flight requests are enough to sustain the rate without the
    // per-message loop itself becoming the bottleneck.
    let concurrency: usize = std::env::var("MQ_PRODUCER_CONCURRENCY")
        .unwrap_or_else(|_| "200".into())
        .parse()?;

    tracing::info!(addr, queue, rate, concurrency, "producer starting");
    // One lazy connection, cloned per publish task below: tonic's client
    // wraps a Channel handle that's cheap to clone and reuses the
    // underlying HTTP/2 connection (established on first use), so this
    // doesn't dial per-message (see mq-raft's Network and mq-node's
    // forward() for the bug this pattern avoids). Publish failures while
    // the target node is still starting up are handled per-attempt below
    // rather than blocking startup on an eager connect.
    let client = MqClient::connect(addr.clone())?;

    let sent = Arc::new(AtomicU64::new(0));
    let failed = Arc::new(AtomicU64::new(0));
    let permits = Arc::new(Semaphore::new(concurrency));

    // Log throughput once/sec instead of per-message - at 1000 msg/s a
    // log line per publish would itself become the bottleneck.
    {
        let sent = sent.clone();
        let failed = failed.clone();
        tokio::spawn(async move {
            let mut interval = tokio::time::interval(Duration::from_secs(1));
            let mut last_sent = 0u64;
            loop {
                interval.tick().await;
                let now_sent = sent.load(Ordering::Relaxed);
                let now_failed = failed.load(Ordering::Relaxed);
                tracing::info!(
                    msgs_per_sec = now_sent - last_sent,
                    total_sent = now_sent,
                    total_failed = now_failed,
                    "throughput"
                );
                last_sent = now_sent;
            }
        });
    }

    let period = Duration::from_nanos(1_000_000_000 / rate.max(1));
    let mut ticker = tokio::time::interval(period);
    ticker.set_missed_tick_behavior(tokio::time::MissedTickBehavior::Burst);

    let mut counter: u64 = 0;
    loop {
        ticker.tick().await;
        counter += 1;

        let permit = permits.clone().acquire_owned().await.unwrap();
        let mut client = client.clone();
        let queue = queue.clone();
        let sent = sent.clone();
        let failed = failed.clone();

        tokio::spawn(async move {
            let _permit = permit;
            let payload = format!("spam message #{counter}").into_bytes();
            match client.publish(&queue, payload).await {
                Ok(_) => {
                    sent.fetch_add(1, Ordering::Relaxed);
                }
                Err(err) => {
                    failed.fetch_add(1, Ordering::Relaxed);
                    tracing::debug!(%err, counter, "publish failed");
                }
            }
        });
    }
}
