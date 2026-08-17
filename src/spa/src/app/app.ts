import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NodeStatusService } from './node-status.service';

@Component({
  selector: 'app-root',
  imports: [CommonModule],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  constructor(protected readonly nodeStatus: NodeStatusService) {}
}
