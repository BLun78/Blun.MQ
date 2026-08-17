var builder = DistributedApplication.CreateBuilder(args);

// Rust workspace root, so `cargo run` resolves the workspace Cargo.toml
// regardless of which crate's binary is being launched.
var rustWorkspaceRoot = Path.GetFullPath(
    Path.Combine(builder.AppHostDirectory, "..", "..", ".."));

// The 3 nodes form a single Raft group for the "spam" partition, so
// every node needs to know every peer's Raft-id -> gRPC-address mapping
// up front (Raft RPCs are multiplexed on the same gRPC port as MqService).
var grpcPorts = new Dictionary<int, int> { [1] = 5001, [2] = 5002, [3] = 5003 };
var peers = string.Join(",", grpcPorts.Select(kv => $"{kv.Key}=http://localhost:{kv.Value}"));

IResourceBuilder<ExecutableResource> AddMqNode(string name, int nodeNum, int grpcPort, int httpPort)
{
    return builder.AddExecutable(name, "cargo", rustWorkspaceRoot,
            "run", "--bin", "mq-node")
        .WithEnvironment("MQ_NODE_ID", name)
        .WithEnvironment("MQ_NODE_NUM", nodeNum.ToString())
        .WithEnvironment("MQ_GRPC_ADDR", $"0.0.0.0:{grpcPort.ToString()}")
        .WithEnvironment("MQ_HTTP_ADDR", $"0.0.0.0:{httpPort.ToString()}")
        .WithEnvironment("MQ_PEERS", peers)
        .WithHttpEndpoint(port: grpcPort, targetPort: grpcPort, name: "grpc", isProxied: false)
        .WithHttpEndpoint(port: httpPort, targetPort: httpPort, name: "http", isProxied: false);
}

var node1 = AddMqNode("node1", nodeNum: 1, grpcPort: 5001, httpPort: 5081);
var node2 = AddMqNode("node2", nodeNum: 2, grpcPort: 5002, httpPort: 5082);
var node3 = AddMqNode("node3", nodeNum: 3, grpcPort: 5003, httpPort: 5083);

// Demo producer publishes 1000 "spam" messages/sec to node 2.
var producer = builder.AddExecutable("producer", "cargo", rustWorkspaceRoot,
        "run", "--bin", "mq-demo-producer")
    .WithEnvironment("MQ_NODE_ADDR", "http://localhost:5002")
    .WithEnvironment("MQ_QUEUE", "spam")
    .WithEnvironment("MQ_RATE", "1000")
    .WaitFor(node2);

// Demo consumer reads the "spam" queue from node 1.
var consumer = builder.AddExecutable("consumer", "cargo", rustWorkspaceRoot,
        "run", "--bin", "mq-demo-consumer")
    .WithEnvironment("MQ_NODE_ADDR", "http://localhost:5001")
    .WithEnvironment("MQ_QUEUE", "spam")
    .WaitFor(node1);

// Second demo consumer, connected to node 3 instead - competing consumer
// for the same "spam" queue, demonstrating that Pop is safe across
// consumers connected to different nodes (each Pop is a Raft proposal,
// so only one of the two ever gets a given message).
var consumer2 = builder.AddExecutable("consumer2", "cargo", rustWorkspaceRoot,
        "run", "--bin", "mq-demo-consumer")
    .WithEnvironment("MQ_NODE_ADDR", "http://localhost:5003")
    .WithEnvironment("MQ_QUEUE", "spam")
    .WaitFor(node3);

// Angular SPA dashboard: shows the "spam" queue depth across all 3 nodes.
// Node status endpoints are fixed localhost ports for this local demo
// scenario (see src/spa/src/app/nodes.config.ts).
var spaDir = Path.Combine(rustWorkspaceRoot, "src", "spa");
var spa = builder.AddNpmApp("spa", spaDir, "start")
    .WithHttpEndpoint(port: 4200, targetPort: 4200, env: "PORT", isProxied: false)
    .WithExternalHttpEndpoints()
    .WaitFor(node1)
    .WaitFor(node2)
    .WaitFor(node3);

builder.Build().Run();
