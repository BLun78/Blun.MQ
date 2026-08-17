use std::collections::BTreeMap;
use std::io::Cursor;
use std::sync::{Arc, Mutex};

use openraft::storage::{RaftSnapshotBuilder, RaftStateMachine};
use openraft::{
    Entry, EntryPayload, LogId, OptionalSend, Snapshot, SnapshotMeta, StorageError,
    StoredMembership,
};

use crate::types::{NodeCommand, NodeResponse, NodeTypeConfig};

/// The cluster membership directory every replica of the "nodes" Raft
/// group converges on: which node ids exist and their gRPC address. This
/// is deliberately separate from `QueueState` (applied by the "spam"
/// group's own state machine) - it's cluster metadata, not queue data.
#[derive(Default, Clone)]
pub struct NodeDirectory {
    pub nodes: BTreeMap<u64, String>,
}

impl NodeDirectory {
    fn apply(&mut self, cmd: NodeCommand) -> NodeResponse {
        match cmd {
            NodeCommand::UpsertNode { id, addr } => {
                self.nodes.insert(id, addr);
            }
        }
        NodeResponse
    }
}

struct StoredSnapshot {
    meta: SnapshotMeta<u64, openraft::BasicNode>,
    data: Vec<u8>,
}

pub struct NodeStateMachineStore {
    pub state: Arc<Mutex<NodeDirectory>>,
    last_applied: Option<LogId<u64>>,
    last_membership: StoredMembership<u64, openraft::BasicNode>,
    snapshot: Option<StoredSnapshot>,
}

impl NodeStateMachineStore {
    pub fn new(state: Arc<Mutex<NodeDirectory>>) -> Self {
        Self {
            state,
            last_applied: None,
            last_membership: StoredMembership::default(),
            snapshot: None,
        }
    }
}

impl RaftSnapshotBuilder<NodeTypeConfig> for NodeStateMachineStore {
    async fn build_snapshot(&mut self) -> Result<Snapshot<NodeTypeConfig>, StorageError<u64>> {
        let nodes = self.state.lock().unwrap().nodes.clone();
        let data = serde_json::to_vec(&nodes).expect("snapshot payload serializes");

        let meta = SnapshotMeta {
            last_log_id: self.last_applied,
            last_membership: self.last_membership.clone(),
            snapshot_id: uuid_like_id(),
        };

        self.snapshot = Some(StoredSnapshot {
            meta: meta.clone(),
            data: data.clone(),
        });

        Ok(Snapshot {
            meta,
            snapshot: Box::new(Cursor::new(data)),
        })
    }
}

fn uuid_like_id() -> String {
    format!(
        "{:x}",
        std::time::SystemTime::now()
            .duration_since(std::time::UNIX_EPOCH)
            .unwrap()
            .as_nanos()
    )
}

impl RaftStateMachine<NodeTypeConfig> for NodeStateMachineStore {
    type SnapshotBuilder = Self;

    async fn applied_state(
        &mut self,
    ) -> Result<
        (
            Option<LogId<u64>>,
            StoredMembership<u64, openraft::BasicNode>,
        ),
        StorageError<u64>,
    > {
        Ok((self.last_applied, self.last_membership.clone()))
    }

    async fn apply<I>(&mut self, entries: I) -> Result<Vec<NodeResponse>, StorageError<u64>>
    where
        I: IntoIterator<Item = Entry<NodeTypeConfig>> + OptionalSend,
        I::IntoIter: OptionalSend,
    {
        let mut responses = Vec::new();
        for entry in entries {
            self.last_applied = Some(entry.log_id);
            let response = match entry.payload {
                EntryPayload::Blank => NodeResponse,
                EntryPayload::Normal(cmd) => self.state.lock().unwrap().apply(cmd),
                EntryPayload::Membership(mem) => {
                    self.last_membership = StoredMembership::new(Some(entry.log_id), mem);
                    NodeResponse
                }
            };
            responses.push(response);
        }
        Ok(responses)
    }

    async fn get_snapshot_builder(&mut self) -> Self::SnapshotBuilder {
        NodeStateMachineStore {
            state: self.state.clone(),
            last_applied: self.last_applied,
            last_membership: self.last_membership.clone(),
            snapshot: None,
        }
    }

    async fn begin_receiving_snapshot(
        &mut self,
    ) -> Result<Box<Cursor<Vec<u8>>>, StorageError<u64>> {
        Ok(Box::new(Cursor::new(Vec::new())))
    }

    async fn install_snapshot(
        &mut self,
        meta: &SnapshotMeta<u64, openraft::BasicNode>,
        snapshot: Box<Cursor<Vec<u8>>>,
    ) -> Result<(), StorageError<u64>> {
        let data = snapshot.into_inner();
        let nodes: BTreeMap<u64, String> =
            serde_json::from_slice(&data).expect("snapshot payload deserializes");

        self.state.lock().unwrap().nodes = nodes;
        self.last_applied = meta.last_log_id;
        self.last_membership = meta.last_membership.clone();
        self.snapshot = Some(StoredSnapshot {
            meta: meta.clone(),
            data,
        });

        Ok(())
    }

    async fn get_current_snapshot(
        &mut self,
    ) -> Result<Option<Snapshot<NodeTypeConfig>>, StorageError<u64>> {
        Ok(self.snapshot.as_ref().map(|s| Snapshot {
            meta: s.meta.clone(),
            snapshot: Box::new(Cursor::new(s.data.clone())),
        }))
    }
}
