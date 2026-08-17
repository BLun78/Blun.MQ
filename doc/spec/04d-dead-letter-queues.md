# Spec: Dead-Letter Queues (DLQ)

> Quelle: [Vision.md](../Vision.md), Abschnitt 4d

## Scope

Automatisches Verschieben nicht zustellbarer Nachrichten in eine DLQ nach
erschöpften Redelivery-Versuchen.

## Anforderungen

- Engine verschiebt Nachrichten automatisch in DLQ, wenn Zustellung nach
  x Redelivery-Versuchen fehlschlägt (x konfigurierbar über den serverweiten
  `mq:redelivery`-Default, siehe [01-transport.md](01-transport.md),
  Konsistenzgarantien). Der Client, der die Queue deklariert
  (`MessageQueueAdmin.DeclareQueue`, siehe [12-declaration.md](12-declaration.md)),
  kann `max_delivery_attempts` und `ack_timeout_ms` optional pro Queue über
  `QueueSpec` überschreiben (unset = serverweiter Default). Die Deklaration ist
  die einzige Stelle, an der das geschieht — nicht mehr `Attach`: `Attach`
  bindet nur noch an eine bereits deklarierte Queue und trägt diese Felder
  nicht mehr (siehe [04f-queue-lifecycle-properties.md](04f-queue-lifecycle-properties.md)
  für die entsprechende Ablösung von `exclusive`/`auto_delete`/`queue_ttl_ms`).
- `Attach.auto_ack`: ein Consumer-Link kann pro Link auf explizites Ack
  verzichten — eine Zustellung gilt als abgeschlossen, sobald der
  `Deliver`-Frame geschrieben wurde, und wird nie redelivert, nie per
  Ack-Timeout erneut zugestellt und nie wegen fehlendem Ack dead-gelettert.
- Namenskonvention: **`{Source-Queue-Name}-DLQ`**, automatisch von der Engine
  angelegt.
- Gilt für Consumer- wie Sync-Queues; bei Consumer Queues ist die DLQ
  ebenfalls node-lokal/transient und folgt demselben Lebenszyklus wie die
  Quell-Queue (siehe [04a-consumer-queues.md](04a-consumer-queues.md)).
- DLQ selbst ist eine reguläre FIFO-Queue und wie jede andere konsumierbar.
