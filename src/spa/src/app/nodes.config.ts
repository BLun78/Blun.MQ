export interface NodeConfig {
  id: string;
  label: string;
  statusStreamUrl: string;
}

// Fixed localhost ports for the local 3-node Aspire demo scenario
// (see src/aspire/Blun.Mq.AppHost/AppHost.cs).
export const NODES: NodeConfig[] = [
  { id: 'node1', label: 'Node 1 (Consumer)', statusStreamUrl: 'http://localhost:5081/status/stream' },
  { id: 'node2', label: 'Node 2 (Producer)', statusStreamUrl: 'http://localhost:5082/status/stream' },
  { id: 'node3', label: 'Node 3', statusStreamUrl: 'http://localhost:5083/status/stream' },
];
