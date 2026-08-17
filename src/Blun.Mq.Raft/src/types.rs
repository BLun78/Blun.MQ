use std::io::Cursor;

use serde::{Deserialize, Serialize};

/// Commands proposed through the "spam" queue's Raft group.
///
/// Both variants are applied deterministically by every replica's state
/// machine in log order, which is what makes `Pop` safe for competing
/// consumers: whichever consumer's proposal commits first gets the
/// message, and every node ends up with the same queue contents.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub enum QueueRequest {
    Publish { queue: String, payload: Vec<u8> },
    Pop { queue: String },
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct QueueResponse {
    pub published_offset: Option<u64>,
    pub popped: Option<(u64, Vec<u8>)>,
}

openraft::declare_raft_types!(
    /// Raft group for the "spam" queue's replicated data. Kept separate
    /// from `NodeTypeConfig` (the cluster-membership group) so the two
    /// groups' logs are independent - see [`crate::node_types`].
    pub QueueTypeConfig:
        D = QueueRequest,
        R = QueueResponse,
        NodeId = u64,
        Node = openraft::BasicNode,
);

/// Commands proposed through the cluster-wide "nodes" Raft group. This
/// group carries only membership/directory data (which node ids exist and
/// where they're reachable) - it has nothing to do with queue payloads,
/// which is exactly why it gets its own Raft group and its own WAL rather
/// than piggy-backing on the "spam" queue's log.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub enum NodeCommand {
    UpsertNode { id: u64, addr: String },
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct NodeResponse;

openraft::declare_raft_types!(
    pub NodeTypeConfig:
        D = NodeCommand,
        R = NodeResponse,
        NodeId = u64,
        Node = openraft::BasicNode,
);
