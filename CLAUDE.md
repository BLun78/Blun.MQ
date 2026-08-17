# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project state

`Blin.MQ.Rust` is a from-scratch Rust rewrite of a message-queuing system, now at a working
clustered prototype stage: a 3-node Raft-replicated queue, driven by a .NET Aspire AppHost, with an
Angular dashboard showing live queue depth across nodes. There's no real WAL yet — storage is
in-memory only (the segmented-log design in the specs isn't implemented).

**Important discrepancy**: `doc/Vision.md` and `doc/spec/*.md` describe the system in terms of a
.NET/C# implementation (gRPC via .NET, `System.Threading.Channels`, FasterLog, dotnext.net.cluster,
etc.) — this is legacy design language from before the Rust pivot. Treat the specs as the source of
truth for *architecture and semantics* (transport model, exchange types, queue types, consistency
guarantees), but translate implementation-detail references (specific .NET APIs/libraries) to Rust
equivalents rather than following them literally. When a spec conflicts with what's actually
implemented, the code wins.

## Repo layout

Cargo workspace at the repo root (`Cargo.toml`), members:

- `src/Blun.Mq.Proto` — tonic/protobuf definitions (`proto/mq.proto`): `MqService` (client-facing
  publish/consume) and `RaftRpc` (inter-node consensus), both multiplexed on the **same gRPC port**
  per node.
- `src/Blun.Mq.Raft` — openraft 0.9.25 integration: `TypeConfig`, `LogStore`, `StateMachineStore`,
  `Network`. Everything is in-memory.
- `src/Blun.Mq.Host` — the node binary (`grpc.rs`, `grpc_raft.rs`, `status_http.rs`, `main.rs`).
  Publish/Consume go through `raft.client_write()`, not a local queue directly. Non-leader nodes
  transparently forward to the leader so clients can always connect to a fixed node.
- `src/Blun.Mq.Client` — thin Rust client library used by the demo producer/consumer.
- `src/demo/{producer,consumer,bench}` — standalone binaries exercising the cluster.
- `src/aspire/Blun.Mq.AppHost` — .NET Aspire orchestration (`AppHost.cs`) that launches node1-3
  (via `cargo run`), the demo producer/consumer, and the Angular SPA (`AddNpmApp`).
- `src/spa` — Angular + Tailwind dashboard (separate npm project, not part of the Cargo workspace).

## Commands

Rust workspace (run from repo root, applies to all crates):
- Build: `cargo build`
- Run a specific node/demo: `cargo run -p <crate-name>` (e.g. `cargo run -p Blun.Mq.Host`)
- Test: `cargo test` (single test: `cargo test <test_name>`)
- Check without building: `cargo check`
- Format: `cargo fmt`
- Lint: `cargo clippy`

Full cluster (3 nodes + producer/consumer + dashboard), via Aspire:
- `dotnet run --project src/aspire/Blun.Mq.AppHost`

Angular SPA (from `src/spa/`): standard Angular CLI (`ng serve`, `ng build`, `ng test`).

## Architecture

The system is a horizontally scalable message broker:

- **Transport**: gRPC/Protobuf over HTTP/2, streaming. Two modes per stream: **Broadcast**
  (fire-and-forget, no ack, no persistence) and **Queue** (at-least-once, ack/redelivery,
  competing consumers). Payloads over 1 MB are referenced via an `ms-link` header instead of
  being transported inline. See `doc/spec/01-transport.md`, `doc/spec/01b-wire-protocol.md`.
- **Exchanges** (`doc/spec/01a-exchanges.md`): producers publish to an Exchange (Direct, Fanout,
  Topic, or Headers type), which routes to bound queues. Bindings are scoped per vhost.
- **Sharding**: each tenant/queue-group gets a virtual host (vhost), which is the sharding unit.
  A directory service maps vhost → shard/node.
- **Replication**: each vhost runs as its own Raft group (1 leader + N followers). Leader election
  is per-vhost, not per-node, so leader load spreads across the cluster. The current prototype runs
  a single 3-node Raft group (one "spam" queue) rather than per-vhost groups.
- **Storage**: target design is an append-only, segmented FIFO log per queue (Kafka-like), with a
  separate priority index (bucket-of-queues per priority level) for priority queues. Not yet
  implemented — current storage is openraft's in-memory log/state machine.
- **Queue types**: Consumer Queues are transient/node-local (exist only while a consumer is
  attached, discarded on disconnect). Sync Queues are persistent, replicated via the vhost's Raft
  group.
- **Scheduled messages** (`doc/spec/04c-scheduled-messages.md`): delayed publish goes into an
  auto-created `{queue}-SMQ`, moved to the target queue when due. Not directly consumable.
- **Dead-letter queues** (`doc/spec/04d-dead-letter-queues.md`): failed redeliveries move to an
  auto-created `{queue}-DLQ` after a configurable retry count. Regular, consumable FIFO queue.
- **Design principles carried over from the vision doc** (apply these when writing Rust, adapted
  idiomatically — e.g. `async`/`await` via Tokio, channels via `tokio::sync::mpsc`/`broadcast`
  instead of `System.Threading.Channels`): streams-only I/O (avoid fully materializing message
  payloads), non-blocking concurrency primitives over locks, async throughout the hot path.

## openraft 0.9.25 gotchas

These cost real debugging time — check them before assuming a compiler error or timeout is a bug
in the app code:

1. `RaftLogStorage`/`RaftStateMachine` (the "v2" storage split API) are **sealed traits** unless the
   `storage-v2` cargo feature is enabled — compiles fine but errors "trait bound Sealed not
   satisfied" otherwise.
2. `RaftNetwork::install_snapshot` (the classic, non-streaming form) is what's actually required to
   implement by default in 0.9.25, not the `full_snapshot` streaming API shown in some docs/newer
   examples — check the compiler's "missing in implementation" error rather than trusting docs.rs
   prose summaries.
3. **Critical**: `RaftNetworkFactory::new_client` is called once per peer and the returned `Network`
   instance is reused for that replication stream's lifetime — do NOT reconnect (`Channel::connect`)
   on every RPC inside `append_entries`/`vote`/etc., it blows past the heartbeat timeout and
   replication permanently times out. Use `Channel::from_shared(addr).connect_lazy()` once in
   `new_client` and clone the channel per call.
4. Reasonable dev-loopback timeouts: `heartbeat_interval: 500`, `election_timeout_min/max:
   1500/3000`. Lower default-ish values (250/800/1500) caused spurious AppendEntries timeouts even
   after fixing gotcha #3's connection churn.
5. Bootstrap pattern used: only the lowest raft-id node calls `raft.initialize(members)` in a retry
   loop (up to 20x/500ms); other nodes just need to be running and reachable — they join via normal
   log replication once a leader is elected. Don't call `initialize` on every node.

## Docs layout

- `doc/Vision.md` — full system vision/architecture (German).
- `doc/spec/*.md` — per-topic specs split out of Vision.md, each linking back to its source
  section. Numbering (`01`, `01a`, `01b`, `04c`, `04d`, ...) mirrors Vision.md section numbers;
  gaps (e.g. no `02-*`, `03-*` yet) mean that section hasn't been split into its own spec file.
