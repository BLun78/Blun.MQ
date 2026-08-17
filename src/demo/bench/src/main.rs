use std::time::Instant;

use mq_client::MqClient;

/// One-off throughput probe: fires MQ_COUNT publishes at a node using
/// MQ_CONCURRENCY parallel connections and reports messages/sec. Not part
/// of the demo scenario - run manually against a live cluster.
#[tokio::main]
async fn main() -> anyhow::Result<()> {
    let addr = std::env::var("MQ_NODE_ADDR").unwrap_or_else(|_| "http://127.0.0.1:5002".into());
    let queue = std::env::var("MQ_QUEUE").unwrap_or_else(|_| "bench".into());
    let count: usize = std::env::var("MQ_COUNT")
        .unwrap_or_else(|_| "500".into())
        .parse()?;
    let concurrency: usize = std::env::var("MQ_CONCURRENCY")
        .unwrap_or_else(|_| "1".into())
        .parse()?;

    println!("target={addr} queue={queue} count={count} concurrency={concurrency}");

    let per_task = count / concurrency;
    let start = Instant::now();

    let mut handles = Vec::new();
    for task_id in 0..concurrency {
        let addr = addr.clone();
        let queue = queue.clone();
        handles.push(tokio::spawn(async move {
            let mut client = MqClient::connect(addr).await.expect("connect");
            for i in 0..per_task {
                let payload = format!("bench-{task_id}-{i}").into_bytes();
                client.publish(&queue, payload).await.expect("publish");
            }
        }));
    }
    for h in handles {
        h.await?;
    }

    let elapsed = start.elapsed();
    let sent = per_task * concurrency;
    let rate = sent as f64 / elapsed.as_secs_f64();
    println!(
        "sent {sent} messages in {:.3}s -> {:.1} msg/s (avg latency {:.2}ms/msg)",
        elapsed.as_secs_f64(),
        rate,
        elapsed.as_secs_f64() * 1000.0 / sent as f64
    );

    Ok(())
}
