# Spec: Delayed / Scheduled Messages (SMQ)

> Quelle: [Vision.md](../Vision.md), Abschnitt 4c

## Scope

Verzögerter/geplanter Publish, sichtbar in einer nicht-konsumierbaren
`-SMQ`-Queue, bis Fälligkeit erreicht ist.

## Anforderungen

- Gilt für beide Queue-Typen (Consumer/Sync) und beide Ordnungssemantiken
  (FIFO/Priority).
- Publish mit Verzögerung, wahlweise:
  - **TimeSpan** (relative Verzögerung), oder
  - **fester Zeitpunkt** (UTC oder lokale Zeit inkl. Zeitzone).
- Nachricht landet sofort in eigener, sichtbarer Queue:
  **`{Target-Queue-Name}-SMQ`**, automatisch von der Engine angelegt (analog
  DLQ-Namenskonvention, siehe [04d-dead-letter-queues.md](04d-dead-letter-queues.md)).
- **Keine Consumer-Bindung an SMQ möglich** — nicht konsumierbar. Inhalt
  (inkl. Fälligkeitszeitpunkt) nur über Admin-Interface einsehbar
  (Monitoring/Debugging, kein regulärer Zustellpfad).
- Nach Ablauf der Verzögerung/Erreichen des Zielzeitpunkts: Engine verschiebt
  Nachricht aus SMQ in Zielqueue — ab dann regulär zustellbar.
- Umsetzung: zeitindizierte Warteliste (Timer-Wheel oder sortierter Index
  nach Fälligkeitszeitpunkt) pro SMQ.
- **Sync Queues**: SMQ wird ebenfalls über die vhost-Raft-Gruppe repliziert
  (kein Scheduling-Verlust bei Leader-Wechsel).
- **Consumer Queues**: SMQ ist transient/node-lokal und teilt das
  Lebenszyklus-Schicksal der zugehörigen Consumer Queue — bei Disconnect
  werden Consumer Queue und SMQ samt aller noch nicht fälligen Nachrichten
  verworfen.

## Worker-Modell

- Eigener, separater Channel-Worker pro SMQ, getrennt vom Consumer-Worker
  (siehe [09-worker-model.md](09-worker-model.md)) — anderer Lebenszyklus,
  kein Consumer-Bezug.
