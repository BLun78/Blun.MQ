use std::sync::{Arc, Mutex};
use std::time::Duration;

use axum::response::IntoResponse;
use axum::response::sse::{Event, Sse};
use axum::routing::get;
use axum::{Json, Router, extract::State};
use futures::stream::Stream;
use serde::Serialize;
use tokio_stream::StreamExt;

use mq_raft::{NodeRaft, QueueRaft, QueueState};

#[derive(Clone)]
pub struct StatusState {
    pub node_id: String,
    pub queue_state: Arc<Mutex<QueueState>>,
    /// Handles to both Raft groups, used only to read `current_leader()` -
    /// this never proposes anything, so it's safe to poll from an
    /// unrelated HTTP handler.
    pub node_raft: NodeRaft,
    pub queue_raft: QueueRaft,
}

#[derive(Serialize)]
struct StatusPayload {
    node_id: String,
    queue_depths: std::collections::HashMap<String, u64>,
    // Raft node id currently leading each group, or null if none elected
    // yet - see doc comment on RaftGroup in proto/mq.proto.
    nodes_group_leader: Option<u64>,
    queue_group_leader: Option<u64>,
}

impl StatusState {
    async fn payload(&self) -> StatusPayload {
        let queue_depths = self.queue_state.lock().unwrap().depths();
        StatusPayload {
            node_id: self.node_id.clone(),
            queue_depths,
            nodes_group_leader: self.node_raft.current_leader().await,
            queue_group_leader: self.queue_raft.current_leader().await,
        }
    }
}

async fn status_once(State(state): State<StatusState>) -> impl IntoResponse {
    Json(state.payload().await)
}

async fn status_stream(
    State(state): State<StatusState>,
) -> Sse<impl Stream<Item = Result<Event, std::convert::Infallible>>> {
    let interval = tokio_stream::wrappers::IntervalStream::new(tokio::time::interval(
        Duration::from_millis(500),
    ));
    let stream = interval.then(move |_| {
        let state = state.clone();
        async move {
            let payload = state.payload().await;
            Ok(Event::default().json_data(&payload).unwrap())
        }
    });
    Sse::new(stream)
}

pub fn router(state: StatusState) -> Router {
    Router::new()
        .route("/status", get(status_once))
        .route("/status/stream", get(status_stream))
        .with_state(state)
        .layer(tower_http::cors::CorsLayer::permissive())
}
