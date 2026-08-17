use std::sync::{Arc, Mutex};
use std::time::Duration;

use axum::response::sse::{Event, Sse};
use axum::response::IntoResponse;
use axum::routing::get;
use axum::{extract::State, Json, Router};
use futures::stream::Stream;
use serde::Serialize;
use tokio_stream::StreamExt;

use mq_raft::QueueState;

#[derive(Clone)]
pub struct StatusState {
    pub node_id: String,
    pub queue_state: Arc<Mutex<QueueState>>,
}

#[derive(Serialize)]
struct StatusPayload {
    node_id: String,
    queue_depths: std::collections::HashMap<String, u64>,
}

async fn status_once(State(state): State<StatusState>) -> impl IntoResponse {
    Json(StatusPayload {
        node_id: state.node_id.clone(),
        queue_depths: state.queue_state.lock().unwrap().depths(),
    })
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
            let payload = StatusPayload {
                node_id: state.node_id.clone(),
                queue_depths: state.queue_state.lock().unwrap().depths(),
            };
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
