use std::collections::BTreeMap;
use std::marker::PhantomData;
use std::sync::Arc;

use openraft::error::{InstallSnapshotError, NetworkError, RPCError, RaftError};
use openraft::network::{RPCOption, RaftNetwork, RaftNetworkFactory};
use openraft::raft::{
    AppendEntriesRequest, AppendEntriesResponse, InstallSnapshotRequest, InstallSnapshotResponse,
    VoteRequest, VoteResponse,
};
use openraft::{BasicNode, RaftTypeConfig};
use tonic::transport::Channel;

use mq_proto::raft_rpc_client::RaftRpcClient;
use mq_proto::{RaftEnvelope, RaftGroup};

/// Maps Raft node ids (1, 2, 3, ...) to the gRPC address every node
/// already listens on for MqService - Raft RPCs are just another
/// service multiplexed on that same port.
pub type PeerAddresses = Arc<BTreeMap<u64, String>>;

/// Generic over the Raft type config `C` so the same network wiring
/// serves both of this crate's Raft groups; `group` tags every outgoing
/// envelope so the receiving node's single `RaftRpc` gRPC service knows
/// which of its two `openraft::Raft` instances to hand the RPC to.
#[derive(Clone)]
pub struct Network<C> {
    peers: PeerAddresses,
    group: RaftGroup,
    _config: PhantomData<fn() -> C>,
}

impl<C> Network<C> {
    pub fn new(peers: PeerAddresses, group: RaftGroup) -> Self {
        Self {
            peers,
            group,
            _config: PhantomData,
        }
    }
}

impl<C: RaftTypeConfig<NodeId = u64, Node = BasicNode>> RaftNetworkFactory<C> for Network<C> {
    type Network = NetworkConnection<C>;

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
            group: self.group,
            _config: PhantomData,
        }
    }
}

pub struct NetworkConnection<C> {
    client: RaftRpcClient<Channel>,
    group: RaftGroup,
    _config: PhantomData<fn() -> C>,
}

fn to_rpc_err<E: std::error::Error + 'static>(err: E) -> RPCError<u64, BasicNode, RaftError<u64>> {
    RPCError::Network(NetworkError::new(&err))
}

impl<C: RaftTypeConfig<NodeId = u64, Node = BasicNode>> RaftNetwork<C> for NetworkConnection<C>
where
    AppendEntriesRequest<C>: serde::Serialize,
    InstallSnapshotRequest<C>: serde::Serialize,
{
    async fn append_entries(
        &mut self,
        rpc: AppendEntriesRequest<C>,
        _option: RPCOption,
    ) -> Result<AppendEntriesResponse<u64>, RPCError<u64, BasicNode, RaftError<u64>>> {
        let payload = serde_json::to_vec(&rpc).expect("serializable");
        let resp = self
            .client
            .clone()
            .append_entries(RaftEnvelope {
                payload,
                group: self.group as i32,
            })
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
            .vote(RaftEnvelope {
                payload,
                group: self.group as i32,
            })
            .await
            .map_err(to_rpc_err)?;
        Ok(serde_json::from_slice(&resp.into_inner().payload).expect("deserializable"))
    }

    async fn install_snapshot(
        &mut self,
        rpc: InstallSnapshotRequest<C>,
        _option: RPCOption,
    ) -> Result<
        InstallSnapshotResponse<u64>,
        RPCError<u64, BasicNode, RaftError<u64, InstallSnapshotError>>,
    > {
        let payload = serde_json::to_vec(&rpc).expect("serializable");
        let resp = self
            .client
            .clone()
            .install_snapshot(RaftEnvelope {
                payload,
                group: self.group as i32,
            })
            .await
            .map_err(|e| RPCError::Network(NetworkError::new(&e)))?;
        Ok(serde_json::from_slice(&resp.into_inner().payload).expect("deserializable"))
    }
}
