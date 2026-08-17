//! Raft log storage backed by [`raft-engine`](https://github.com/tikv/raft-engine),
//! TiKV's segmented, checksummed, batch-fsync WAL for Multi-Raft logs. This
//! replaces the purely in-memory `LogStore` for anything meant to survive a
//! process restart - openraft still owns replication/consensus, this crate
//! just gives it durable storage for the log and the vote.
//!
//! Generic over the Raft type config `C` so the same store backs both of
//! this crate's Raft groups (the "nodes" directory group and the "spam"
//! queue's data group, see [`crate::types`]). Each group gets its own
//! `RaftEngineLogStore::open`ed at a distinct directory, which keeps their
//! WALs fully separate on disk - one group's log replay/purge/compaction
//! never touches the other's files.
//!
//! raft-engine's public API is protobuf-oriented (its `MessageExt::Entry`
//! must implement `protobuf::Message`), so every openraft `Entry<C>` is
//! wrapped in a small `Blob { index, data }` protobuf envelope (generated
//! from `proto/raft_log_entry.proto`) whose `data` is just the entry
//! serde_json-encoded. raft-engine only inspects `index`; the payload is
//! opaque to it.
//!
//! raft-engine's own I/O is synchronous, so every call is dispatched via
//! `tokio::task::spawn_blocking` to keep it off the async executor.

use std::marker::PhantomData;
use std::ops::RangeBounds;
use std::sync::Arc;

use anyerror::AnyError;
use openraft::storage::{LogFlushed, LogState, RaftLogReader, RaftLogStorage};
use openraft::{LogId, OptionalSend, RaftTypeConfig, StorageError, StorageIOError, Vote};
use raft_engine::{Config, Engine, LogBatch, MessageExt};

use crate::pb::Blob;

/// A single raft-engine instance only ever backs one Raft group here (see
/// the module docs), so there's only ever one region in use per instance.
/// A future per-vhost Raft group setup that shares one engine across many
/// groups would use the vhost id as the region id instead.
const REGION_ID: u64 = 1;

const VOTE_KEY: &[u8] = b"vote";
const LAST_PURGED_KEY: &[u8] = b"last_purged";
const COMMITTED_KEY: &[u8] = b"committed";

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

fn encode_entry<C: RaftTypeConfig>(entry: &openraft::Entry<C>) -> Result<Blob, StorageError<u64>>
where
    openraft::Entry<C>: serde::Serialize,
{
    let data = serde_json::to_vec(entry).map_err(to_storage_err)?;
    let mut blob = Blob::new();
    blob.set_index(entry.log_id.index);
    blob.set_data(data);
    Ok(blob)
}

fn decode_entry<C: RaftTypeConfig>(blob: &Blob) -> Result<openraft::Entry<C>, StorageError<u64>>
where
    openraft::Entry<C>: serde::de::DeserializeOwned,
{
    serde_json::from_slice(blob.get_data()).map_err(to_storage_err)
}

pub struct RaftEngineLogStore<C> {
    engine: Arc<Engine>,
    _config: PhantomData<fn() -> C>,
}

impl<C> Clone for RaftEngineLogStore<C> {
    fn clone(&self) -> Self {
        Self {
            engine: self.engine.clone(),
            _config: PhantomData,
        }
    }
}

impl<C: RaftTypeConfig<NodeId = u64>> RaftEngineLogStore<C> {
    /// Opens (creating if necessary) a raft-engine instance rooted at `dir`.
    /// Give each Raft group its own `dir` to keep their WALs separate.
    pub fn open(dir: impl Into<String>) -> anyhow::Result<Self> {
        let dir = dir.into();
        // raft-engine only creates the leaf directory itself, not any
        // missing parents - needed here since two groups now share a
        // parent `data_dir` (`{data_dir}/nodes`, `{data_dir}/spam`).
        std::fs::create_dir_all(&dir)?;
        let config = Config {
            dir,
            ..Default::default()
        };
        let engine = Engine::open(config)?;
        Ok(Self {
            engine: Arc::new(engine),
            _config: PhantomData,
        })
    }

    fn get_blob_message(&self, key: &'static [u8]) -> Result<Option<Blob>, StorageError<u64>> {
        self.engine
            .get_message::<Blob>(REGION_ID, key)
            .map_err(to_storage_err)
    }
}

impl<C: RaftTypeConfig<NodeId = u64, Entry = openraft::Entry<C>>> RaftLogReader<C>
    for RaftEngineLogStore<C>
where
    openraft::Entry<C>: serde::Serialize + serde::de::DeserializeOwned,
{
    async fn try_get_log_entries<RB: RangeBounds<u64> + Clone + std::fmt::Debug + OptionalSend>(
        &mut self,
        range: RB,
    ) -> Result<Vec<openraft::Entry<C>>, StorageError<u64>> {
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

        blobs.iter().map(decode_entry::<C>).collect()
    }
}

impl<C: RaftTypeConfig<NodeId = u64, Entry = openraft::Entry<C>>> RaftLogStorage<C>
    for RaftEngineLogStore<C>
where
    openraft::Entry<C>: serde::Serialize + serde::de::DeserializeOwned,
{
    type LogReader = Self;

    async fn get_log_state(&mut self) -> Result<LogState<C>, StorageError<u64>> {
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
                    .map(|blob| decode_entry::<C>(&blob))
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

    // openraft's default RaftLogStorage::save_committed/read_committed are
    // no-ops that always return None - fine as long as nothing else is
    // persisted, but we *do* persist `purge()`'s last-purged log id, and
    // openraft's startup validation requires `purge_upto <= committed`.
    // Leaving committed un-persisted meant every restart after a purge
    // panicked ("purge_upto <= committed(None)" invariant violation), so
    // it has to be tracked durably here too, the same way vote is.
    async fn save_committed(&mut self, committed: Option<LogId<u64>>) -> Result<(), StorageError<u64>> {
        let data = serde_json::to_vec(&committed).map_err(to_storage_err)?;
        let mut blob = Blob::new();
        blob.set_data(data);

        let engine = self.engine.clone();
        tokio::task::spawn_blocking(move || {
            let mut batch = LogBatch::default();
            batch.put_message(REGION_ID, COMMITTED_KEY.to_vec(), &blob)?;
            engine.write(&mut batch, true)?;
            Ok::<_, raft_engine::Error>(())
        })
        .await
        .map_err(to_storage_err)?
        .map_err(to_storage_err)
    }

    async fn read_committed(&mut self) -> Result<Option<LogId<u64>>, StorageError<u64>> {
        self.get_blob_message(COMMITTED_KEY)?
            .map(|blob| serde_json::from_slice(blob.get_data()))
            .transpose()
            .map_err(to_storage_err)
            .map(|opt: Option<Option<LogId<u64>>>| opt.flatten())
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
        callback: LogFlushed<C>,
    ) -> Result<(), StorageError<u64>>
    where
        I: IntoIterator<Item = openraft::Entry<C>> + OptionalSend,
        I::IntoIter: OptionalSend,
    {
        let blobs = entries
            .into_iter()
            .map(|e| encode_entry::<C>(&e))
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
