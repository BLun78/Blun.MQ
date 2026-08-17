# Spec: Transport-Schicht

> Quelle: [Vision.md](../Vision.md), Abschnitt 1

## Scope

gRPC/Protobuf-basierter Transport über HTTP/2 für Publish, Deliver, Ack/Nack und
Flow-Control, in zwei Betriebsmodi (Broadcast, Queue).

## Anforderungen

- **Protokoll**: gRPC (bidirektionales Streaming über HTTP/2), Protobuf als
  Message-Format. Kein eigenes Framing.
- **Streams**: ein Stream pro Queue-Subscription bzw. Broadcast-Kanal; mehrere
  Streams über eine TCP-Verbindung (HTTP/2-Multiplexing).
- **Contracts** (`.proto`):
  - Publish (Producer → Server)
  - Deliver + Ack/Nack (Server ↔ Consumer)
  - Flow-Control (Credit-based, über gRPC-Nachrichten, nicht HTTP/2-Frames)
- **Nachrichtengröße**: hart limitiert auf 1 MB Payload.
  - Größere Payloads: kein Inline-Transport, stattdessen Header **`ms-link`**
    (`{uri/string to source}`), der auf externen Speicherort verweist.
  - Nachricht enthält dann nur Metadaten + `ms-link`; Auflösung ist Consumer-/
    Client-seitig (siehe [10-client.md](10-client.md)).
- **Betriebsmodi**:
  - **Broadcast**: fire-and-forget, kein Ack, kein Replay, keine Persistenz.
  - **Queue**: at-least-once, Ack/Redelivery, Consumer-Groups (competing
    consumers).

## Konsistenzgarantien

- **Broadcast (best-effort)**:
  - Kein Consumer verbunden → Nachricht geht verloren, kein Replay.
  - Server bestätigt Producer nur die *Annahme*, nicht die Zustellung.
  - Kein Ordering-Anspruch über mehrere Consumer hinweg.
- **Queue (at-least-once)**:
  - "Zugestellt" = committed beim Raft-Leader der vhost-Gruppe
    (Mehrheits-Ack, siehe [03-replication.md](03-replication.md)) — unabhängig
    vom Consumer-Ack.
  - Zusätzliches Consumer-Ack nach Verarbeitung erwartet; Timeout →
    Redelivery.
  - Redelivery kann Duplikate erzeugen (at-least-once, nicht exactly-once) —
    Konsumenten müssen idempotent verarbeiten, falls relevant.
  - Nach x fehlgeschlagenen Redelivery-Versuchen → DLQ (siehe
    [04d-dead-letter-queues.md](04d-dead-letter-queues.md)) statt endlosem
    Retry.

## Implementierungsprinzipien (bindend, siehe Abschnitt 7)

- Kein `ReadAllBytes`/`ToArray()` auf dem Hot-Path.
- `PipeReader`/`PipeWriter`, `Span<byte>`/`ReadOnlySequence<byte>`, gRPCs
  `IAsyncStreamReader`/`IServerStreamWriter` direkt statt vollständiger
  Message-Materialisierung.

## Offene Fragen

- Exaktes Framing-Protokoll (Message-Format, Ack-Semantik, Flow-Control-Details)
  ist noch nicht spezifiziert (siehe Vision.md, "Offene Fragen").
