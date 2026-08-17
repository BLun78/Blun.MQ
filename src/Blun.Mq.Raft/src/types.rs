use std::io::Cursor;

use serde::{Deserialize, Serialize};

/// Commands proposed through the "spam" partition's Raft group.
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
    pub TypeConfig:
        D = QueueRequest,
        R = QueueResponse,
        NodeId = u64,
        Node = openraft::BasicNode,
);
