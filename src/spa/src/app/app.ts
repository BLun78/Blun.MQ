import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NodeStatusService } from './node-status.service';
import { labelForRaftId } from './nodes.config';

@Component({
  selector: 'app-root',
  imports: [CommonModule],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  protected readonly labelForRaftId = labelForRaftId;

  constructor(protected readonly nodeStatus: NodeStatusService) {}
}
