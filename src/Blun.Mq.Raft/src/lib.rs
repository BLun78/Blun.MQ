pub mod log_store;
pub mod network;
pub mod state_machine;
pub mod types;

pub use log_store::LogStore;
pub use network::{Network, NetworkConnection, PeerAddresses};
pub use state_machine::{QueueState, StateMachineStore};
pub use types::{QueueRequest, QueueResponse, TypeConfig};

pub type Raft = openraft::Raft<TypeConfig>;

pub fn raft_config() -> openraft::Config {
    openraft::Config {
        heartbeat_interval: 500,
        election_timeout_min: 1500,
        election_timeout_max: 3000,
        ..Default::default()
    }
}
