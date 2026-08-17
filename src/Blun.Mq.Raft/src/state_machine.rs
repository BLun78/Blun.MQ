use std::collections::{HashMap, VecDeque};
use std::io::Cursor;
use std::sync::{Arc, Mutex};

use openraft::storage::{RaftSnapshotBuilder, RaftStateMachine};
use openraft::{
    Entry, EntryPayload, LogId, OptionalSend, Snapshot, SnapshotMeta, StorageError,
    StoredMembership,
};

use crate::types::{QueueRequest, QueueResponse, QueueTypeConfig};

/// The queue state every replica converges on. Shared (via `Arc<Mutex<_>>`)
/// with the node's gRPC/HTTP layers so status reads never have to go
/// through Raft - only writes (Publish/Pop) do.
#[derive(Default)]
pub struct QueueState {
    pub queues: HashMap<String, VecDeque<(u64, bytes::Bytes)>>,
    next_offset: HashMap<String, u64>,
}

impl QueueState {
    pub fn depths(&self) -> HashMap<String, u64> {
        self.queues
            .iter()
            .map(|(name, q)| (name.clone(), q.len() as u64))
            .collect()
    }

    fn apply(&mut self, req: QueueRequest) -> QueueResponse {
        match req {
            QueueRequest::Publish { queue, payload } => {
                let offset_counter = self.next_offset.entry(queue.clone()).or_insert(0);
                let offset = *offset_counter;
                *offset_counter += 1;
                self.queues
                    .entry(queue)
                    .or_default()
                    .push_back((offset, bytes::Bytes::from(payload)));
                QueueResponse {
                    published_offset: Some(offset),
                    popped: None,
                }
            }
            QueueRequest::Pop { queue } => {
                let popped = self
                    .queues
                    .get_mut(&queue)
                    .and_then(|q| q.pop_front())
                    .map(|(offset, payload)| (offset, payload.to_vec()));
                QueueResponse {
                    published_offset: None,
                    popped,
                }
            }
        }
    }
}

struct StoredSnapshot {
    meta: SnapshotMeta<u64, openraft::BasicNode>,
    data: Vec<u8>,
}

#[derive(serde::Serialize, serde::Deserialize)]
struct SnapshotPayload {
    queues: HashMap<String, VecDeque<(u64, Vec<u8>)>>,
    next_offset: HashMap<String, u64>,
}

pub struct StateMachineStore {
    pub state: Arc<Mutex<QueueState>>,
    last_applied: Option<LogId<u64>>,
    last_membership: StoredMembership<u64, openraft::BasicNode>,
    snapshot: Option<StoredSnapshot>,
}

impl StateMachineStore {
    pub fn new(state: Arc<Mutex<QueueState>>) -> Self {
        Self {
            state,
            last_applied: None,
            last_membership: StoredMembership::default(),
            snapshot: None,
        }
    }
}

impl RaftSnapshotBuilder<QueueTypeConfig> for StateMachineStore {
    async fn build_snapshot(&mut self) -> Result<Snapshot<QueueTypeConfig>, StorageError<u64>> {
        let payload = {
            let state = self.state.lock().unwrap();
            SnapshotPayload {
                queues: state
                    .queues
                    .iter()
                    .map(|(k, v)| (k.clone(), v.iter().map(|(o, p)| (*o, p.to_vec())).collect()))
                    .collect(),
                next_offset: state.next_offset.clone(),
            }
        };
        let data = serde_json::to_vec(&payload).expect("snapshot payload serializes");

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

impl RaftStateMachine<QueueTypeConfig> for StateMachineStore {
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

    async fn apply<I>(&mut self, entries: I) -> Result<Vec<QueueResponse>, StorageError<u64>>
    where
        I: IntoIterator<Item = Entry<QueueTypeConfig>> + OptionalSend,
        I::IntoIter: OptionalSend,
    {
        let mut responses = Vec::new();
        for entry in entries {
            self.last_applied = Some(entry.log_id);
            let response = match entry.payload {
                EntryPayload::Blank => QueueResponse::default(),
                EntryPayload::Normal(req) => self.state.lock().unwrap().apply(req),
                EntryPayload::Membership(mem) => {
                    self.last_membership = StoredMembership::new(Some(entry.log_id), mem);
                    QueueResponse::default()
                }
            };
            responses.push(response);
        }
        Ok(responses)
    }

    async fn get_snapshot_builder(&mut self) -> Self::SnapshotBuilder {
        StateMachineStore {
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
        let payload: SnapshotPayload =
            serde_json::from_slice(&data).expect("snapshot payload deserializes");

        {
            let mut state = self.state.lock().unwrap();
            state.queues = payload
                .queues
                .into_iter()
                .map(|(k, v)| {
                    (
                        k,
                        v.into_iter()
                            .map(|(o, p)| (o, bytes::Bytes::from(p)))
                            .collect(),
                    )
                })
                .collect();
            state.next_offset = payload.next_offset;
        }

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
    ) -> Result<Option<Snapshot<QueueTypeConfig>>, StorageError<u64>> {
        Ok(self.snapshot.as_ref().map(|s| Snapshot {
            meta: s.meta.clone(),
            snapshot: Box::new(Cursor::new(s.data.clone())),
        }))
    }
}
