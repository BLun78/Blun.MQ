use std::collections::BTreeMap;
use std::sync::Arc;

use openraft::error::{InstallSnapshotError, NetworkError, RPCError, RaftError};
use openraft::network::{RPCOption, RaftNetwork, RaftNetworkFactory};
use openraft::raft::{
    AppendEntriesRequest, AppendEntriesResponse, InstallSnapshotRequest, InstallSnapshotResponse,
    VoteRequest, VoteResponse,
};
use openraft::BasicNode;
use tonic::transport::Channel;

use mq_proto::raft_rpc_client::RaftRpcClient;
use mq_proto::RaftEnvelope;

use crate::types::TypeConfig;

/// Maps Raft node ids (1, 2, 3, ...) to the gRPC address every node
/// already listens on for MqService - Raft RPCs are just another
/// service multiplexed on that same port.
pub type PeerAddresses = Arc<BTreeMap<u64, String>>;

#[derive(Clone)]
pub struct Network {
    peers: PeerAddresses,
}

impl Network {
    pub fn new(peers: PeerAddresses) -> Self {
        Self { peers }
    }
}

impl RaftNetworkFactory<TypeConfig> for Network {
    type Network = NetworkConnection;

    async fn new_client(&mut self, target: u64, _node: &BasicNode) -> Self::Network {
        let addr = self
            .peers
            .get(&target)
            .cloned()
            .unwrap_or_else(|| panic!("unknown raft peer id {target}"));
        // connect_lazy doesn't dial immediately (and can't fail here);
        // the actual TCP+HTTP2 handshake happens on first RPC and the
        // channel is then reused for the lifetime of this replication
        // stream instead of reconnecting on every heartbeat.
        let channel = Channel::from_shared(addr)
            .expect("valid grpc address")
            .connect_lazy();
        NetworkConnection {
            client: RaftRpcClient::new(channel),
        }
    }
}

pub struct NetworkConnection {
    client: RaftRpcClient<Channel>,
}

fn to_rpc_err<E: std::error::Error + 'static>(
    err: E,
) -> RPCError<u64, BasicNode, RaftError<u64>> {
    RPCError::Network(NetworkError::new(&err))
}

impl RaftNetwork<TypeConfig> for NetworkConnection {
    async fn append_entries(
        &mut self,
        rpc: AppendEntriesRequest<TypeConfig>,
        _option: RPCOption,
    ) -> Result<AppendEntriesResponse<u64>, RPCError<u64, BasicNode, RaftError<u64>>> {
        let payload = serde_json::to_vec(&rpc).expect("serializable");
        let resp = self
            .client
            .clone()
            .append_entries(RaftEnvelope { payload })
            .await
            .map_err(to_rpc_err)?;
        Ok(serde_json::from_slice(&resp.into_inner().payload).expect("deserializable"))
    }

    async fn vote(
        &mut self,
        rpc: VoteRequest<u64>,
        _option: RPCOption,
    ) -> Result<VoteResponse<u64>, RPCError<u64, BasicNode, RaftError<u64>>> {
        let payload = serde_json::to_vec(&rpc).expect("serializable");
        let resp = self
            .client
            .clone()
            .vote(RaftEnvelope { payload })
            .await
            .map_err(to_rpc_err)?;
        Ok(serde_json::from_slice(&resp.into_inner().payload).expect("deserializable"))
    }

    async fn install_snapshot(
        &mut self,
        rpc: InstallSnapshotRequest<TypeConfig>,
        _option: RPCOption,
    ) -> Result<InstallSnapshotResponse<u64>, RPCError<u64, BasicNode, RaftError<u64, InstallSnapshotError>>>
    {
        let payload = serde_json::to_vec(&rpc).expect("serializable");
        let resp = self
            .client
            .clone()
            .install_snapshot(RaftEnvelope { payload })
            .await
            .map_err(|e| RPCError::Network(NetworkError::new(&e)))?;
        Ok(serde_json::from_slice(&resp.into_inner().payload).expect("deserializable"))
    }
}
