use std::collections::BTreeMap;
use std::ops::RangeBounds;
use std::sync::{Arc, Mutex};

use openraft::storage::{LogFlushed, LogState, RaftLogReader, RaftLogStorage};
use openraft::{LogId, OptionalSend, StorageError, Vote};

use crate::types::TypeConfig;

/// Purely in-memory Raft log. Nothing here survives a process restart -
/// acceptable for this demo's Consumer-Queue-style semantics, but a real
/// Sync Queue deployment would back this with the segmented WAL described
/// in doc/spec instead.
#[derive(Clone, Default)]
pub struct LogStore {
    inner: Arc<Mutex<LogStoreInner>>,
}

#[derive(Default)]
struct LogStoreInner {
    log: BTreeMap<u64, openraft::Entry<TypeConfig>>,
    vote: Option<Vote<u64>>,
    last_purged: Option<LogId<u64>>,
}

impl RaftLogReader<TypeConfig> for LogStore {
    async fn try_get_log_entries<RB: RangeBounds<u64> + Clone + std::fmt::Debug + OptionalSend>(
        &mut self,
        range: RB,
    ) -> Result<Vec<openraft::Entry<TypeConfig>>, StorageError<u64>> {
        let inner = self.inner.lock().unwrap();
        Ok(inner.log.range(range).map(|(_, v)| v.clone()).collect())
    }
}

impl RaftLogStorage<TypeConfig> for LogStore {
    type LogReader = Self;

    async fn get_log_state(&mut self) -> Result<LogState<TypeConfig>, StorageError<u64>> {
        let inner = self.inner.lock().unwrap();
        let last_log_id = inner.log.values().next_back().map(|e| e.log_id);
        Ok(LogState {
            last_purged_log_id: inner.last_purged,
            last_log_id,
        })
    }

    async fn get_log_reader(&mut self) -> Self::LogReader {
        self.clone()
    }

    async fn save_vote(&mut self, vote: &Vote<u64>) -> Result<(), StorageError<u64>> {
        self.inner.lock().unwrap().vote = Some(*vote);
        Ok(())
    }

    async fn read_vote(&mut self) -> Result<Option<Vote<u64>>, StorageError<u64>> {
        Ok(self.inner.lock().unwrap().vote)
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
        {
            let mut inner = self.inner.lock().unwrap();
            for entry in entries {
                inner.log.insert(entry.log_id.index, entry);
            }
        }
        callback.log_io_completed(Ok(()));
        Ok(())
    }

    async fn truncate(&mut self, log_id: LogId<u64>) -> Result<(), StorageError<u64>> {
        let mut inner = self.inner.lock().unwrap();
        let tail = inner.log.split_off(&log_id.index);
        drop(tail);
        Ok(())
    }

    async fn purge(&mut self, log_id: LogId<u64>) -> Result<(), StorageError<u64>> {
        let mut inner = self.inner.lock().unwrap();
        inner.log.retain(|index, _| *index > log_id.index);
        inner.last_purged = Some(log_id);
        Ok(())
    }
}
