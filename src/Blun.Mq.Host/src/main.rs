mod grpc;
mod grpc_raft;
mod status_http;

use std::collections::BTreeMap;
use std::net::SocketAddr;
use std::sync::{Arc, Mutex};
use std::time::Duration;

use mq_proto::mq_service_server::MqServiceServer;
use mq_proto::raft_rpc_server::RaftRpcServer;
use mq_raft::{Network, QueueState, RaftEngineLogStore, StateMachineStore};
use tracing_subscriber::EnvFilter;

use grpc::MqServiceImpl;
use grpc_raft::RaftRpcImpl;

/// Parses "1=http://localhost:5001,2=http://localhost:5002,3=http://localhost:5003".
fn parse_peers(s: &str) -> BTreeMap<u64, String> {
    s.split(',')
        .filter(|part| !part.trim().is_empty())
        .map(|part| {
            let (id, addr) = part
                .split_once('=')
                .unwrap_or_else(|| panic!("invalid MQ_PEERS entry: {part}"));
            (
                id.trim().parse().expect("peer id must be a u64"),
                addr.trim().to_string(),
            )
        })
        .collect()
}

#[tokio::main]
async fn main() -> anyhow::Result<()> {
    tracing_subscriber::fmt()
        .with_env_filter(EnvFilter::from_default_env().add_directive("info".parse()?))
        .init();

    let node_id = std::env::var("MQ_NODE_ID").unwrap_or_else(|_| "node-1".to_string());
    let node_num: u64 = std::env::var("MQ_NODE_NUM")
        .unwrap_or_else(|_| "1".to_string())
        .parse()?;
    let grpc_addr: SocketAddr = std::env::var("MQ_GRPC_ADDR")
        .unwrap_or_else(|_| "0.0.0.0:5001".to_string())
        .parse()?;
    let http_addr: SocketAddr = std::env::var("MQ_HTTP_ADDR")
        .unwrap_or_else(|_| "0.0.0.0:5080".to_string())
        .parse()?;
    let peers = parse_peers(
        &std::env::var("MQ_PEERS")
            .unwrap_or_else(|_| format!("1=http://localhost:{}", grpc_addr.port())),
    );
    let peers = Arc::new(peers);
    let data_dir = std::env::var("MQ_DATA_DIR").unwrap_or_else(|_| format!("./data/{node_id}"));

    tracing::info!(node_id, node_num, %grpc_addr, %http_addr, ?peers, data_dir, "starting mq-node");

    // --- Raft wiring for the "spam" partition's Raft group ---
    let queue_state = Arc::new(Mutex::new(QueueState::default()));
    // raft-engine gives us a durable, segmented WAL for the Raft log, so the
    // log (and the vote) survive a node restart - only the applied
    // QueueState above is still in-memory and gets rebuilt by replaying the
    // log on startup, same as any Raft-backed state machine.
    let log_store = RaftEngineLogStore::open(data_dir)?;
    let state_machine = StateMachineStore::new(queue_state.clone());
    let network = Network::new(peers.clone());
    let raft = mq_raft::Raft::new(
        node_num,
        Arc::new(mq_raft::raft_config().validate()?),
        network,
        log_store,
        state_machine,
    )
    .await?;

    // Only the lowest-numbered node bootstraps the cluster; the others
    // just need to be reachable, they join via normal log replication
    // once a leader is elected. Safe to call repeatedly: openraft errors
    // (rather than panics) once the cluster is already initialized.
    let lowest_id = *peers.keys().next().expect("MQ_PEERS must not be empty");
    if node_num == lowest_id {
        let raft = raft.clone();
        let peers = peers.clone();
        tokio::spawn(async move {
            let members: BTreeMap<u64, openraft::BasicNode> = peers
                .iter()
                .map(|(id, addr)| (*id, openraft::BasicNode::new(addr.clone())))
                .collect();
            for attempt in 1..=20 {
                match raft.initialize(members.clone()).await {
                    Ok(()) => {
                        tracing::info!("raft cluster initialized");
                        break;
                    }
                    Err(err) => {
                        tracing::debug!(attempt, ?err, "raft initialize not ready yet");
                        tokio::time::sleep(Duration::from_millis(500)).await;
                    }
                }
            }
        });
    }

    let status_state = status_http::StatusState {
        node_id: node_id.clone(),
        queue_state,
    };

    let mq_service = MqServiceImpl {
        node_id,
        raft: raft.clone(),
        state: status_state.queue_state.clone(),
        peers,
        forward_channels: Arc::new(Mutex::new(std::collections::HashMap::new())),
    };

    let grpc_server = tonic::transport::Server::builder()
        .add_service(MqServiceServer::new(mq_service))
        .add_service(RaftRpcServer::new(RaftRpcImpl { raft }))
        .serve(grpc_addr);

    let http_router = status_http::router(status_state);
    let http_listener = tokio::net::TcpListener::bind(http_addr).await?;
    let http_server = axum::serve(http_listener, http_router);

    tokio::select! {
        res = grpc_server => res?,
        res = http_server => res?,
    }

    Ok(())
}
