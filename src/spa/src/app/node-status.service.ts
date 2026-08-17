import { Injectable, signal } from '@angular/core';
import { NODES, NodeConfig } from './nodes.config';

export interface NodeStatus {
  config: NodeConfig;
  connected: boolean;
  queueDepths: Record<string, number>;
  nodesGroupLeader?: number;
  queueGroupLeader?: number;
}

interface StatusUpdatePayload {
  node_id: string;
  queue_depths: Record<string, number>;
  nodes_group_leader?: number;
  queue_group_leader?: number;
}

@Injectable({ providedIn: 'root' })
export class NodeStatusService {
  readonly statuses = signal<Record<string, NodeStatus>>(
    Object.fromEntries(
      NODES.map((config) => [config.id, { config, connected: false, queueDepths: {} }]),
    ),
  );

  constructor() {
    for (const config of NODES) {
      this.connect(config);
    }
  }

  private connect(config: NodeConfig): void {
    const source = new EventSource(config.statusStreamUrl);

    source.onopen = () => this.patch(config.id, { connected: true });
    source.onerror = () => this.patch(config.id, { connected: false });
    source.onmessage = (event) => {
      const payload: StatusUpdatePayload = JSON.parse(event.data);
      this.patch(config.id, {
        connected: true,
        queueDepths: payload.queue_depths,
        nodesGroupLeader: payload.nodes_group_leader,
        queueGroupLeader: payload.queue_group_leader,
      });
    };
  }

  private patch(id: string, partial: Partial<NodeStatus>): void {
    this.statuses.update((current) => ({
      ...current,
      [id]: { ...current[id], ...partial },
    }));
  }
}
