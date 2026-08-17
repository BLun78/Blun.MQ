use std::fs;
use std::path::Path;

fn main() {
    println!("cargo:rerun-if-changed=proto/raft_log_entry.proto");
    let out_dir = std::env::var("OUT_DIR").unwrap();
    protobuf_codegen_pure::Codegen::new()
        .out_dir(&out_dir)
        .include("proto")
        .input("proto/raft_log_entry.proto")
        .run()
        .expect("protobuf codegen for the raft-engine entry envelope failed");

    // protobuf-codegen emits `#![allow(...)]` / `//!` crate-level attributes
    // meant for a file used directly as a module (`#[path = "..."] mod x;`),
    // which can't take a computed OUT_DIR path on stable Rust. Strip them so
    // the file can instead be pulled in with `include!` from a real module.
    let generated = Path::new(&out_dir).join("raft_log_entry.rs");
    let content = fs::read_to_string(&generated).expect("read generated raft_log_entry.rs");
    let cleaned: String = content
        .lines()
        .filter(|line| !line.starts_with("#!") && !line.starts_with("//!"))
        .collect::<Vec<_>>()
        .join("\n");
    fs::write(&generated, cleaned).expect("rewrite generated raft_log_entry.rs");
}
