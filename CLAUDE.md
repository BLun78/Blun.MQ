# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project state

This repo (`Blin.MQ.Rust`) is a from-scratch Rust rewrite of a message-queuing system. Code is at
the initial-scaffold stage: `src/Blun.Mq.Host` is a bare `cargo new` binary crate (`main.rs` still
prints "Hello, world!", `Cargo.toml` has no dependencies yet). There is no workspace `Cargo.toml` at
the repo root — `src/Blun.Mq.Host` is currently the only crate.

**Important discrepancy**: `doc/Vision.md` and `doc/spec/*.md` describe the system in terms of a
.NET/C# implementation (gRPC via .NET, `System.Threading.Channels`, FasterLog, dotnext.net.cluster,
Angular admin UI, etc.) — this is legacy design language from before the Rust pivot. Treat the specs
as the source of truth for *architecture and semantics* (transport model, exchange types, queue
types, consistency guarantees), but translate implementation-detail references (specific .NET APIs/
libraries) to Rust equivalents rather than following them literally. When a spec conflicts with
what's actually implemented, the code wins.

## Commands

Work from `src/Blun.Mq.Host/`:
- Build: `cargo build`
- Run: `cargo run`
- Test: `cargo test` (single test: `cargo test <test_name>`)
- Check without building: `cargo check`
- Format: `cargo fmt`
- Lint: `cargo clippy`

## Architecture (target design, per doc/Vision.md — not yet implemented)

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
  is per-vhost, not per-node, so leader load spreads across the cluster.
- **Storage**: append-only, segmented FIFO log per queue (Kafka-like), with a separate priority
  index (bucket-of-queues per priority level) for priority queues.
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

## Docs layout

- `doc/Vision.md` — full system vision/architecture (German).
- `doc/spec/*.md` — per-topic specs split out of Vision.md, each linking back to its source
  section. Numbering (`01`, `01a`, `01b`, `04c`, `04d`, ...) mirrors Vision.md section numbers;
  gaps (e.g. no `02-*`, `03-*` yet) mean that section hasn't been split into its own spec file.
