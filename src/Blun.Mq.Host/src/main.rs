mod grpc;
mod grpc_raft;
mod status_http;

use std::collections::BTreeMap;
use std::net::SocketAddr;
use std::sync::{Arc, Mutex};
use std::time::Duration;

use mq_proto::RaftGroup;
use mq_proto::mq_service_server::MqServiceServer;
use mq_proto::raft_rpc_server::RaftRpcServer;
use mq_raft::{
    Network, NodeDirectory, NodeStateMachineStore, QueueState, RaftEngineLogStore,
    StateMachineStore,
};
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

/// Repeatedly calls `raft.initialize(members)` until it succeeds (or gives
/// up). Only the lowest-numbered node does this per group; the others just
/// need to be reachable, they join via normal log replication once a
/// leader is elected. Safe to call on an already-initialized cluster:
/// openraft errors (rather than panics) in that case.
async fn bootstrap<C>(raft: openraft::Raft<C>, peers: BTreeMap<u64, String>, group: &'static str)
where
    C: openraft::RaftTypeConfig<NodeId = u64, Node = openraft::BasicNode>,
{
    tokio::spawn(async move {
        let members: BTreeMap<u64, openraft::BasicNode> = peers
            .iter()
            .map(|(id, addr)| (*id, openraft::BasicNode::new(addr.clone())))
            .collect();
        for attempt in 1..=20 {
            match raft.initialize(members.clone()).await {
                Ok(()) => {
                    tracing::info!(group, "raft cluster initialized");
                    break;
                }
                Err(err) => {
                    tracing::debug!(group, attempt, ?err, "raft initialize not ready yet");
                    tokio::time::sleep(Duration::from_millis(500)).await;
                }
            }
        }
    });
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

    // --- Two independent Raft groups, each with its own WAL ---
    //
    // "nodes": cluster membership/directory data - which node ids exist
    // and where they're reachable. Low-churn (only changes when a node
    // joins/leaves).
    //
    // "spam": the actual queue's message data - every publish/pop is a
    // proposal against this group. High-churn, the real workload.
    //
    // Splitting them means queue write traffic never contends with (or
    // gets blocked behind) membership-group log I/O, and vice versa -
    // each group's WAL is a separate raft-engine instance rooted at its
    // own subdirectory, so their on-disk log segments never mix.
    let node_directory = Arc::new(Mutex::new(NodeDirectory::default()));
    let node_log_store =
        RaftEngineLogStore::<mq_raft::NodeTypeConfig>::open(format!("{data_dir}/nodes"))?;
    let node_state_machine = NodeStateMachineStore::new(node_directory.clone());
    let node_network = Network::<mq_raft::NodeTypeConfig>::new(peers.clone(), RaftGroup::Nodes);
    let node_raft = mq_raft::NodeRaft::new(
        node_num,
        Arc::new(mq_raft::raft_config().validate()?),
        node_network,
        node_log_store,
        node_state_machine,
    )
    .await?;

    let queue_state = Arc::new(Mutex::new(QueueState::default()));
    // raft-engine gives us a durable, segmented WAL for the Raft log, so the
    // log (and the vote) survive a node restart - only the applied
    // QueueState above is still in-memory and gets rebuilt by replaying the
    // log on startup, same as any Raft-backed state machine.
    let queue_log_store =
        RaftEngineLogStore::<mq_raft::QueueTypeConfig>::open(format!("{data_dir}/spam"))?;
    let queue_state_machine = StateMachineStore::new(queue_state.clone());
    let queue_network = Network::<mq_raft::QueueTypeConfig>::new(peers.clone(), RaftGroup::Spam);
    let queue_raft = mq_raft::QueueRaft::new(
        node_num,
        Arc::new(mq_raft::raft_config().validate()?),
        queue_network,
        queue_log_store,
        queue_state_machine,
    )
    .await?;

    // Only the lowest-numbered node bootstraps each group's cluster.
    let lowest_id = *peers.keys().next().expect("MQ_PEERS must not be empty");
    if node_num == lowest_id {
        bootstrap(node_raft.clone(), (*peers).clone(), "nodes").await;
        bootstrap(queue_raft.clone(), (*peers).clone(), "spam").await;
    }

    // Every node registers itself in the "nodes" directory group once it's
    // up - a real (if minimal) use of that group, replicated independently
    // of anything queue-related.
    {
        let node_raft = node_raft.clone();
        let node_id_str = node_id.clone();
        let self_addr = peers
            .get(&node_num)
            .cloned()
            .unwrap_or_else(|| grpc_addr.to_string());
        tokio::spawn(async move {
            loop {
                match node_raft
                    .client_write(mq_raft::NodeCommand::UpsertNode {
                        id: node_num,
                        addr: self_addr.clone(),
                    })
                    .await
                {
                    Ok(_) => {
                        tracing::info!(node_id = node_id_str, "registered self in nodes directory");
                        break;
                    }
                    Err(err) => {
                        tracing::debug!(?err, "nodes directory not ready yet, retrying");
                        tokio::time::sleep(Duration::from_millis(500)).await;
                    }
                }
            }
        });
    }

    let status_state = status_http::StatusState {
        node_id: node_id.clone(),
        queue_state,
        node_raft: node_raft.clone(),
        queue_raft: queue_raft.clone(),
    };

    let mq_service = MqServiceImpl {
        node_id,
        raft: queue_raft.clone(),
        node_raft: node_raft.clone(),
        state: status_state.queue_state.clone(),
        peers,
        forward_channels: Arc::new(Mutex::new(std::collections::HashMap::new())),
    };

    let grpc_server = tonic::transport::Server::builder()
        .add_service(MqServiceServer::new(mq_service))
        .add_service(RaftRpcServer::new(RaftRpcImpl {
            node_raft,
            queue_raft,
        }))
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
