export interface NodeConfig {
  id: string;
  label: string;
  statusStreamUrl: string;
  // Raft node id (MQ_NODE_NUM) this config corresponds to - used to map
  // the numeric leader ids reported by /status back onto a node's label.
  raftId: number;
}

// Fixed localhost ports for the local 3-node Aspire demo scenario
// (see src/aspire/Blun.Mq.AppHost/AppHost.cs).
export const NODES: NodeConfig[] = [
  { id: 'node1', label: 'Node 1 (Consumer)', statusStreamUrl: 'http://localhost:5081/status/stream', raftId: 1 },
  { id: 'node2', label: 'Node 2 (Producer)', statusStreamUrl: 'http://localhost:5082/status/stream', raftId: 2 },
  { id: 'node3', label: 'Node 3 (Consumer)', statusStreamUrl: 'http://localhost:5083/status/stream', raftId: 3 },
];

export function labelForRaftId(raftId: number | undefined): string | undefined {
  return NODES.find((n) => n.raftId === raftId)?.label;
}
