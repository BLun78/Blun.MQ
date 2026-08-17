//! Raft log storage backed by [`raft-engine`](https://github.com/tikv/raft-engine),
//! TiKV's segmented, checksummed, batch-fsync WAL for Multi-Raft logs. This
//! replaces the purely in-memory `LogStore` for anything meant to survive a
//! process restart - openraft still owns replication/consensus, this crate
//! just gives it durable storage for the log and the vote.
//!
//! raft-engine's public API is protobuf-oriented (its `MessageExt::Entry`
//! must implement `protobuf::Message`), so every openraft `Entry<TypeConfig>`
//! is wrapped in a small `Blob { index, data }` protobuf envelope (generated
//! from `proto/raft_log_entry.proto`) whose `data` is just the entry
//! serde_json-encoded. raft-engine only inspects `index`; the payload is
//! opaque to it.
//!
//! raft-engine's own I/O is synchronous, so every call is dispatched via
//! `tokio::task::spawn_blocking` to keep it off the async executor.

use std::ops::RangeBounds;
use std::sync::Arc;

use anyerror::AnyError;
use openraft::storage::{LogFlushed, LogState, RaftLogReader, RaftLogStorage};
use openraft::{LogId, OptionalSend, StorageError, StorageIOError, Vote};
use raft_engine::{Config, Engine, LogBatch, MessageExt};

use crate::types::TypeConfig;

use crate::pb::Blob;

/// All log entries live under a single raft-engine "region" - this
/// prototype runs one Raft group (the "spam" partition) per node, so there's
/// no need to shard by vhost/queue yet. A future per-vhost Raft group setup
/// would use the vhost id as the region id instead.
const REGION_ID: u64 = 1;

const VOTE_KEY: &[u8] = b"vote";
const LAST_PURGED_KEY: &[u8] = b"last_purged";

#[derive(Clone)]
struct BlobEntry;

impl MessageExt for BlobEntry {
    type Entry = Blob;

    fn index(e: &Self::Entry) -> u64 {
        e.index
    }
}

fn to_storage_err<E: std::error::Error + 'static>(err: E) -> StorageError<u64> {
    StorageError::IO {
        source: StorageIOError::write(AnyError::new(&err)),
    }
}

fn encode_entry(entry: &openraft::Entry<TypeConfig>) -> Result<Blob, StorageError<u64>> {
    let data = serde_json::to_vec(entry).map_err(to_storage_err)?;
    let mut blob = Blob::new();
    blob.set_index(entry.log_id.index);
    blob.set_data(data);
    Ok(blob)
}

fn decode_entry(blob: &Blob) -> Result<openraft::Entry<TypeConfig>, StorageError<u64>> {
    serde_json::from_slice(blob.get_data()).map_err(to_storage_err)
}

#[derive(Clone)]
pub struct RaftEngineLogStore {
    engine: Arc<Engine>,
}

impl RaftEngineLogStore {
    /// Opens (creating if necessary) a raft-engine instance rooted at `dir`.
    pub fn open(dir: impl Into<String>) -> anyhow::Result<Self> {
        let config = Config {
            dir: dir.into(),
            ..Default::default()
        };
        let engine = Engine::open(config)?;
        Ok(Self {
            engine: Arc::new(engine),
        })
    }

    fn get_blob_message(&self, key: &'static [u8]) -> Result<Option<Blob>, StorageError<u64>> {
        self.engine
            .get_message::<Blob>(REGION_ID, key)
            .map_err(to_storage_err)
    }
}

impl RaftLogReader<TypeConfig> for RaftEngineLogStore {
    async fn try_get_log_entries<RB: RangeBounds<u64> + Clone + std::fmt::Debug + OptionalSend>(
        &mut self,
        range: RB,
    ) -> Result<Vec<openraft::Entry<TypeConfig>>, StorageError<u64>> {
        let start = match range.start_bound() {
            std::ops::Bound::Included(&s) => s,
            std::ops::Bound::Excluded(&s) => s + 1,
            std::ops::Bound::Unbounded => 0,
        };
        let end = match range.end_bound() {
            std::ops::Bound::Included(&e) => e + 1,
            std::ops::Bound::Excluded(&e) => e,
            std::ops::Bound::Unbounded => u64::MAX,
        };

        let engine = self.engine.clone();
        let blobs = tokio::task::spawn_blocking(move || {
            let mut blobs = Vec::new();
            engine.fetch_entries_to::<BlobEntry>(REGION_ID, start, end, None, &mut blobs)?;
            Ok::<_, raft_engine::Error>(blobs)
        })
        .await
        .map_err(to_storage_err)?
        .map_err(to_storage_err)?;

        blobs.iter().map(decode_entry).collect()
    }
}

impl RaftLogStorage<TypeConfig> for RaftEngineLogStore {
    type LogReader = Self;

    async fn get_log_state(&mut self) -> Result<LogState<TypeConfig>, StorageError<u64>> {
        let last_purged_log_id = self
            .get_blob_message(LAST_PURGED_KEY)?
            .map(|blob| serde_json::from_slice(blob.get_data()))
            .transpose()
            .map_err(to_storage_err)?;

        let engine = self.engine.clone();
        let last_index = engine.last_index(REGION_ID);
        let last_log_id = match last_index {
            Some(idx) => {
                let entry = tokio::task::spawn_blocking(move || {
                    engine.get_entry::<BlobEntry>(REGION_ID, idx)
                })
                .await
                .map_err(to_storage_err)?
                .map_err(to_storage_err)?;
                entry
                    .map(|blob| decode_entry(&blob))
                    .transpose()?
                    .map(|e| e.log_id)
            }
            None => None,
        };

        Ok(LogState {
            last_purged_log_id,
            last_log_id: last_log_id.or(last_purged_log_id),
        })
    }

    async fn get_log_reader(&mut self) -> Self::LogReader {
        self.clone()
    }

    async fn save_vote(&mut self, vote: &Vote<u64>) -> Result<(), StorageError<u64>> {
        let data = serde_json::to_vec(vote).map_err(to_storage_err)?;
        let mut blob = Blob::new();
        blob.set_data(data);

        let engine = self.engine.clone();
        tokio::task::spawn_blocking(move || {
            let mut batch = LogBatch::default();
            batch.put_message(REGION_ID, VOTE_KEY.to_vec(), &blob)?;
            engine.write(&mut batch, true)?;
            Ok::<_, raft_engine::Error>(())
        })
        .await
        .map_err(to_storage_err)?
        .map_err(to_storage_err)
    }

    async fn read_vote(&mut self) -> Result<Option<Vote<u64>>, StorageError<u64>> {
        self.get_blob_message(VOTE_KEY)?
            .map(|blob| serde_json::from_slice(blob.get_data()))
            .transpose()
            .map_err(to_storage_err)
    }

    async fn append<I>(
        &mut self,
        entries: I,
        callback: LogFlushed<TypeConfig>,
    ) -> Result<(), StorageError<u64>>
    where
        I: IntoIterator<Item = openraft::Entry<TypeConfig>> + OptionalSend,
        I::IntoIter: OptionalSend,
    {
        let blobs = entries
            .into_iter()
            .map(|e| encode_entry(&e))
            .collect::<Result<Vec<_>, _>>()?;

        let engine = self.engine.clone();
        let io_result: Result<(), raft_engine::Error> = tokio::task::spawn_blocking(move || {
            let mut batch = LogBatch::with_capacity(blobs.len());
            batch.add_entries::<BlobEntry>(REGION_ID, &blobs)?;
            // sync=true: every batch is fsync'd before the callback fires,
            // matching openraft's expectation that a completed append is
            // durable (this is the WAL's whole point).
            engine.write(&mut batch, true)?;
            Ok(())
        })
        .await
        .unwrap_or_else(|join_err| {
            Err(raft_engine::Error::Other(Box::new(std::io::Error::other(
                join_err.to_string(),
            ))))
        });

        match io_result {
            Ok(()) => {
                callback.log_io_completed(Ok(()));
                Ok(())
            }
            Err(err) => {
                callback.log_io_completed(Err(std::io::Error::other(err.to_string())));
                Err(to_storage_err(err))
            }
        }
    }

    async fn truncate(&mut self, log_id: LogId<u64>) -> Result<(), StorageError<u64>> {
        // raft-engine truncates the conflicting suffix automatically the
        // next time entries are appended starting at `log_id.index` (see
        // `MemTable::prepare_append`), so there's nothing to do eagerly
        // here beyond what the following `append` call will already cause.
        let _ = log_id;
        Ok(())
    }

    async fn purge(&mut self, log_id: LogId<u64>) -> Result<(), StorageError<u64>> {
        let data = serde_json::to_vec(&log_id).map_err(to_storage_err)?;
        let mut blob = Blob::new();
        blob.set_data(data);

        let engine = self.engine.clone();
        let index = log_id.index;
        tokio::task::spawn_blocking(move || {
            let mut batch = LogBatch::default();
            batch.put_message(REGION_ID, LAST_PURGED_KEY.to_vec(), &blob)?;
            engine.write(&mut batch, true)?;
            engine.compact_to(REGION_ID, index + 1);
            Ok::<_, raft_engine::Error>(())
        })
        .await
        .map_err(to_storage_err)?
        .map_err(to_storage_err)
    }
}
