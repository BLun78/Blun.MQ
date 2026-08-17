# Spec: Wire-Protokoll (AMQP-inspiriert über gRPC)

> Quelle: [Vision.md](../Vision.md), Abschnitt 1 · Ergänzt [01-transport.md](01-transport.md),
> beantwortet dessen offene Frage nach dem "exakten Framing-Protokoll".

## Scope

Konkretes Nachrichten-/Framing-Format für die gRPC-Transportschicht aus
[01-transport.md](01-transport.md). Angelehnt an die AMQP-0-9-1-/1.0-Semantik
(Connection/Channel/Performative-Modell), aber **ein einzelner bidirektionaler
gRPC-Stream** statt eigenem TCP-Framing: Senden (Publish, Ack, Flow-Control)
und Empfangen (Deliver, Flow-Control, Close) laufen über denselben Stream,
analog dazu, wie AMQP-Performatives auf einer Connection in beide Richtungen
multiplext werden.

## AMQP → gRPC: Begriffs-Mapping

| AMQP-Konzept                     | Entsprechung in diesem Protokoll                                              |
|-----------------------------------|--------------------------------------------------------------------------------|
| Connection                        | HTTP/2-Verbindung (1 TCP-Connection, per OIDC-Token authentifiziert)          |
| Channel                            | gRPC bidi-Stream (`rpc Connect`), 1 Stream = 1 logischer Channel              |
| `open`/`close` (Connection)        | `Open`/`Closed`-Frame (siehe unten), einmal pro Stream-Lebenszyklus           |
| `begin`/`end` (Session)            | entfällt — 1 Stream = 1 Session, kein verschachteltes Session-Layer nötig     |
| `attach` (Link auf Exchange/Queue) | `Attach`-Frame: bindet den Stream an Exchange+Routing-Key oder an eine Queue  |
| `transfer` (Nachrichtenübertragung)| `Publish`-Frame (Client→Server) / `Deliver`-Frame (Server→Client)             |
| `flow` (Credit-based Flow-Control) | `Flow`-Frame (Server→Client: Credit gewähren; Client→Server: Credit anfordern)|
| `disposition` (Ack/Nack/Reject)    | `Ack`-Frame (Client→Server), `ack`/`nack`/`reject` als `Outcome`-Enum          |
| `detach`                           | `Detach`-Frame: Consumer löst sich von einer Queue, Stream bleibt offen       |

Ein gRPC-Stream entspricht also einer AMQP-Connection **mit genau einer
Session**, kann aber über `Attach`/`Detach` mehrere Links (Publish- und/oder
Consume-Bindings) gleichzeitig führen — Multiplexing über mehrere Streams
bleibt zusätzlich über HTTP/2 möglich (siehe [01-transport.md](01-transport.md)),
ist aber pro Client optional (z.B. ein Stream pro Queue-Subscription, wenn
unabhängige Flow-Control pro Queue gewünscht ist).

## Frame-Modell

Ein einziger bidirektionaler RPC trägt in beide Richtungen ein
`oneof`-Envelope-Frame — analog zum AMQP-Performative-Set, aber auf die für
dieses System relevante Teilmenge reduziert:

```protobuf
syntax = "proto3";
package blun.mq.v1;

service MessageQueue {
  // Ein Stream pro Channel (siehe Mapping-Tabelle). Client und Server senden
  // unabhängig voneinander auf demselben Stream (volles Duplex, kein
  // Request/Response-Ping-Pong).
  rpc Connect(stream ClientFrame) returns (stream ServerFrame);
}

// ---- Client → Server ----
message ClientFrame {
  oneof frame {
    Open      open      = 1;
    Attach    attach    = 2;
    Detach    detach    = 3;
    Publish   publish   = 4;
    Ack       ack       = 5;
    Flow      flow      = 6;   // Consumer fordert Credit an (Pull-Modell)
    Close     close      = 7;
    Ping      ping      = 8;   // Liveness, s. u.
    Pong      pong      = 9;
  }
}

// ---- Server → Client ----
message ServerFrame {
  oneof frame {
    Opened    opened    = 1;
    Attached  attached  = 2;
    Detached  detached  = 3;
    Deliver   deliver   = 4;
    Flow      flow      = 5;   // Server gewährt Credit
    Closed    closed    = 6;
    Error     error     = 7;
    Published published = 8;  // nur bei nicht-trivialem Publish-Ausgang, s. u.
    Ping      ping      = 9;   // Liveness, s. u.
    Pong      pong      = 10;
  }
}
```

### Connection-Lifecycle

```protobuf
message Open {
  string vhost           = 1;  // Ziel-vhost (Sharding-Key, siehe 02-sharding.md)
  string client_id       = 2;  // frei wählbar, für Logging/Tracing
  map<string, string> properties = 3; // z.B. Client-Version, Feature-Flags
}

message Opened {
  string server_id       = 1;
  uint32 max_frame_bytes = 2;  // = 1 MB Payload-Limit (siehe 01-transport.md)
}

message Close {
  string reason = 1;
}

message Closed {
  string reason = 1;
}

message Error {
  string code    = 1;  // z.B. "VHOST_NOT_FOUND", "UNAUTHORIZED", "QUEUE_NOT_FOUND"
  string message = 2;
  bool   fatal   = 3;  // true => Stream wird server-seitig geschlossen
}
```

### Attach — Binding an Exchange oder Queue

`Attach` deckt sowohl Producer- als auch Consumer-Bindings ab; die Rolle
ergibt sich aus `role`:

```protobuf
enum LinkRole {
  LINK_ROLE_UNSPECIFIED = 0;
  PRODUCER              = 1;  // publiziert an ein Exchange
  CONSUMER               = 2;  // konsumiert aus einer Queue
}

enum DeliveryMode {
  DELIVERY_MODE_UNSPECIFIED = 0;
  BROADCAST                 = 1;  // fire-and-forget, siehe 01-transport.md
  QUEUE                     = 2;  // at-least-once mit Ack/Redelivery
}

message Attach {
  // sync, exclusive, auto_delete, queue_ttl_ms, max_delivery_attempts und
  // ack_timeout_ms ritten früher hier mit — first-Attach-wins bzw. inkonsistentes
  // Merging zweier Deklaranten, siehe 12-declaration.md, Abschnitt "Motivation".
  // Reserviert, nicht wiederverwendbar. Queue-Eigenschaften werden ausschließlich
  // über QueueSpec auf MessageQueueAdmin.DeclareQueue deklariert
  // (12-declaration.md); Attach bindet nur noch an eine bereits deklarierte Queue.
  reserved 8, 16, 17, 18, 20, 21;
  reserved "sync", "exclusive", "auto_delete", "queue_ttl_ms",
           "max_delivery_attempts", "ack_timeout_ms";

  string       link_id  = 1;  // client-generierte ID, referenziert in Publish/Deliver/Flow
  LinkRole     role     = 2;
  DeliveryMode mode     = 3;

  // PRODUCER: Ziel-Exchange + optionaler Routing-Key (Direct/Topic) bzw.
  // Header-Match-Kriterium (Headers-Exchange, siehe 01a-exchanges.md). Leerer
  // exchange = Default-Exchange, routing_key ist dann direkt der Ziel-Queue-Name
  // (siehe 01a-exchanges.md, Abschnitt "Umsetzung").
  string       exchange    = 4;
  string       routing_key = 5;

  // CONSUMER: Ziel-Queue (Consumer- oder Sync-Queue, siehe 04a/04b). Muss bereits
  // deklariert sein (12-declaration.md) — Attach erzeugt keine Queue.
  string       queue           = 6;
  string       consumer_group  = 7; // competing consumers, leer = exklusiv

  // PRODUCER: opt-in auf Publisher-Confirmation — jeder Publish wird dann mit
  // genau einem Published-Frame beantwortet (siehe unten, "Publisher-Confirmation").
  bool         confirm = 9;

  // PRODUCER: Per-Link-Defaults für Nachrichten-Metadaten, die ein Producer fast
  // immer wiederholt. Der Broker füllt sie beim Publish dort ein, wo die Nachricht
  // das Feld leer ließ; ein von der Nachricht selbst gesetzter Wert gewinnt immer.
  string               default_content_type     = 10;
  string               default_content_encoding = 11;
  string               default_type             = 12;
  string               default_app_id           = 13;
  string               default_reply_to         = 14;
  map<string, string>  default_headers          = 15;

  // CONSUMER: eine Zustellung gilt als abgeschlossen, sobald der Deliver-Frame
  // geschrieben wurde — kein Ack erwartet, keine Redelivery, kein Ack-Timeout,
  // kein Dead-Lettering wegen fehlendem Ack (siehe 04d-dead-letter-queues.md).
  // Default false.
  bool         auto_ack = 19;
}

message Attached {
  string link_id = 1;
  bool   ok      = 2;
}

message Detach {
  string link_id = 1;
}

message Detached {
  string link_id = 1;
  string reason  = 2;
}
```

### Publish / Deliver — Nachrichtentransfer

```protobuf
// Orientiert an den AMQP-0-9-1 Basic Properties — ohne `delivery_mode`:
// Durability ist hier eine Queue-Eigenschaft (QueueSpec.durability, deklariert
// über MessageQueueAdmin.DeclareQueue, siehe 12-declaration.md), keine
// Per-Message-Eigenschaft.
message Message {
  string               message_id = 1;  // server-seitig vergeben falls leer
  bytes                payload    = 2;  // <= 1 MB, sonst ms-link (siehe unten)
  map<string, string>  headers    = 3;  // inkl. optionalem ms-link, he-match-Zielattribute
  uint32               priority   = 4;  // 0–9, siehe 04-storage.md (Bucket-Index)
  google.protobuf.Timestamp scheduled_for = 5; // optional, siehe 04c-scheduled-messages.md

  string content_type     = 6;   // MIME-Typ des Bodys, z. B. "application/json"
  string content_encoding = 7;   // z. B. "gzip", wenn der Payload komprimiert ist
  string correlation_id   = 8;   // Request/Reply-Korrelation
  string reply_to         = 9;   // Queue-Name für die Antwort
  uint64 ttl_ms           = 10;  // TTL ab Broker-Annahme; 0 = kein Ablauf, siehe 04d
  google.protobuf.Timestamp timestamp = 11; // Erzeugungszeit, vom Producer gesetzt
  string type             = 12;  // anwendungsdefinierter Nachrichtentyp
  string user_id          = 13;  // server-validiert aus dem OIDC-Subject, siehe 05-auth.md
  string app_id           = 14;  // erzeugende Anwendung
  string deduplication_id = 15;  // optionaler Dedup-Schlüssel, siehe unten
}

message Publish {
  string  link_id = 1;
  Message message = 2;
}

message Deliver {
  string  link_id      = 1;
  Message message      = 2;
  uint64  delivery_tag = 3;  // eindeutig pro Link, referenziert in Ack
  bool    redelivered  = 4;
}
```

- **`ttl_ms`**: läuft ab Broker-Annahme (nicht ab `timestamp` des Producers).
  Eine abgelaufene Nachricht wird beim Dequeue nicht mehr zugestellt, sondern mit
  Grund `expired` in die DLQ verschoben ([04d](04d-dead-letter-queues.md)) — sie
  verbraucht dabei kein Consumer-Credit.
- **`user_id`**: wird serverseitig überschrieben, nie vom Producer übernommen —
  ein Consumer kann sich deshalb darauf verlassen.
- **`content_type` / `content_encoding` / `type` / `app_id`**: rein deskriptiv, der
  Broker interpretiert weder sie noch den Body (opaker Byte-Array).
- **`deduplication_id`**: die Ziel-Queue merkt sich diese ID für ihr deklariertes
  **Deduplizierungsfenster** (`QueueSpec.deduplication_retention_ms`, siehe
  [12-declaration.md](12-declaration.md)), gerechnet ab dem Reinschreiben. Jeder
  weitere Publish mit derselben ID innerhalb des Fensters wird verworfen — still,
  ohne `Error`-Frame (aus Producer-Sicht ein erfolgreiches No-op). Leer = keine
  Deduplizierung. Für die Eindeutigkeit sorgt der Producer; der Broker erkennt
  nur Kollisionen und vergibt selbst nie eine ID.
  - Das Gedächtnis überlebt die Nachricht: Zustellen, Quittieren
    (`Ack(ACCEPTED)`) oder Verschieben in die DLQ geben die ID **nicht** frei, nur
    der Ablauf des Fensters tut das. Zwischen zwei Publishes derselben ID ist die
    Queue im Normalfall leer — genau dann muss das Gedächtnis noch da sein.
  - Damit bleibt die ID auch über Redelivery hinweg belegt, ohne dass das ein
    eigener Sonderfall wäre.
  - Der Geltungsbereich ist die einzelne Queue, nicht der Vhost: dasselbe
    `deduplication_id` in zwei verschiedenen Queues sind zwei verschiedene
    Nachrichten. Die DLQ führt ihren eigenen ID-Raum; ein Dead-Letter-Move
    reserviert die ID dort erneut und wird verworfen, wenn die DLQ sie noch kennt.
  - Das Gedächtnis hängt am Queue-*Namen*, nicht am Queue-Objekt: baut der Server
    die Queue selbst ab (Queue TTL, Auto-Delete, weggebrochene Exclusive-
    Verbindung, Neustart), bleibt es bestehen und wird beim erneuten Deklarieren
    desselben Namens wieder wirksam. Ein Purge leert es.
  - Ob der Producer vom Verwerfen erfährt, hängt an `Attach.confirm` (siehe unten):
    ohne Confirm ist der Drop für ihn nicht von einer Annahme zu unterscheiden —
    was für Fire-and-Forget genau richtig ist, denn beides bedeutet „diese ID ist
    im aktuellen Fenster genau einmal angekommen".
- **`ms-link`-Header**: wird wie in [01-transport.md](01-transport.md) als
  Eintrag in `Message.headers["ms-link"]` transportiert, kein eigenes
  Proto-Feld — hält den Envelope schlank und erlaubt weitere Header ohne
  Schema-Änderung.
- **Broadcast-Publish**: Server antwortet nicht mit `Deliver` an den
  Producer, sondern quittiert nur die Annahme (kein eigenes Frame nötig — das
  Ausbleiben eines `Error`-Frames auf dem Stream *ist* die Annahme-Bestätigung,
  analog AMQP `settled`-Transfers ohne Disposition).
- **`Published`-Frame**: die eine Ausnahme davon — und nur für Producer-Links mit
  `Attach.confirm = true`:

  ```protobuf
  enum PublishStatus {
    PUBLISH_STATUS_UNSPECIFIED = 0;
    PUBLISH_ACCEPTED           = 1;  // geroutet
    PUBLISH_DUPLICATE          = 2;  // wegen deduplication_id verworfen
  }

  message Published {
    string        link_id    = 1;
    string        message_id = 2;
    PublishStatus status     = 3;
  }
  ```

  `PUBLISH_DUPLICATE` ist ausdrücklich **kein Fehler**: die frühere Nachricht
  steht, die Absicht des Producers ist erfüllt, und er darf **nicht** erneut
  publishen. Deshalb ein eigenes Frame statt `Error` — ein Producer, der auf
  `Error` mit Retry reagiert, würde sonst in einer Schleife landen.
- **Publisher-Confirmation (`Attach.confirm`)**: opt-in pro PRODUCER-Link, und die
  einzige Bedingung, unter der überhaupt `Published`-Frames fließen. Es gibt genau
  zwei Betriebsarten:
  - **`confirm = false` (Default)**: der Link ist vollständig fire-and-forget. Kein
    `Published`-Frame, weder für die Annahme noch für den Dedup-Drop — nur echte
    Fehler kommen als `Error` zurück. Der häufige Pfad bleibt so frei von
    Request/Response-Ping-Pong.
  - **`confirm = true`**: **jeder** Publish wird mit genau einem `Published`-Frame
    beantwortet — `PUBLISH_ACCEPTED` beim Routen, `PUBLISH_DUPLICATE` beim
    Verwerfen. Für Producer, die erst nach der Bestätigung weiterarbeiten dürfen,
    und die einzige Möglichkeit, „eingereiht" von „als Duplikat verworfen" zu
    unterscheiden.
  - Bei Sync Queues bestätigt `PUBLISH_ACCEPTED` derzeit nur das **Anhängen an den
    Raft-Log**, nicht den Commit — siehe [ISSUES.md](../ISSUES.md) #6.

### Ack/Nack — Disposition

```protobuf
enum Outcome {
  OUTCOME_UNSPECIFIED = 0;
  ACCEPTED             = 1;  // Ack: Verarbeitung erfolgreich
  REJECTED             = 2;  // Nack ohne Redelivery-Wunsch -> direkt Richtung DLQ-Zähler
  RELEASED              = 3;  // Nack mit Redelivery-Wunsch (z.B. Consumer überlastet)
}

message Ack {
  string  link_id      = 1;
  uint64  delivery_tag = 2;  // korrespondiert zu Deliver.delivery_tag
  Outcome outcome      = 3;
}
```

Nur für `DeliveryMode.QUEUE` relevant (siehe Konsistenzgarantien in
[01-transport.md](01-transport.md)); bei `BROADCAST` sendet der Server keine
`Deliver.delivery_tag`-Erwartung und der Client sollte kein `Ack` senden
(wird server-seitig ignoriert, falls doch gesendet).

- **Ein `Ack` betrifft genau eine Nachricht.** Es gibt bewusst kein
  `multiple`-Flag wie in AMQP 0-9-1 und keine „bis-einschließlich"-Semantik:
  `delivery_tag` adressiert eine einzelne ausstehende Zustellung, und alle
  übrigen bleiben unberührt. Hat ein Consumer 10 Nachrichten offen und schickt
  für eine davon `REJECTED`, wandert nur diese in die DLQ — die anderen neun
  laufen weiter ihren eigenen Weg (`ACCEPTED`, `RELEASED` oder Ack-Timeout),
  jede für sich. Das ist die Voraussetzung dafür, dass ein Consumer mit
  `prefetch > 1` mehrere Nachrichten *nebenläufig* verarbeiten und in
  beliebiger Reihenfolge quittieren darf; ein Sammel-Ack würde diese Freiheit
  wieder wegnehmen, weil er Nachrichten mitbestätigt, deren Ausgang der Client
  noch gar nicht kennt.
- **FIFO gilt für die Queue, nicht für den Lebenslauf einer Nachricht.**
  `RELEASED` (und ein Ack-Timeout) reiht die Nachricht neu ein, sie erscheint
  also typischerweise *hinter* den bereits ausgelieferten. Eine
  Redelivery-Reihenfolge relativ zu ihren früheren Nachbarn ist ausdrücklich
  nicht zugesichert — wer strikte Ordnung braucht, konsumiert mit
  `prefetch = 1` und akzeptiert den Durchsatzverlust.

### Flow — Credit-based Flow-Control

```protobuf
message Flow {
  string link_id = 1;
  uint32 credit  = 2;  // Client→Server: neues absolutes Credit-Ziel für
                         // diesen Link, keine Delta-Menge. Server→Client:
                         // gewährtes Kontingent, informativ.
}
```

- Pull-artiges Modell: Consumer sendet initial (bei `Attach`) implizit
  `credit = 0`; erst ein `Flow`-Frame vom Client gibt dem Server das Budget,
  weitere `Deliver`-Frames auf diesem `link_id` zu senden.
- **`credit` ist ein absoluter Zielwert, kein Delta.** Ein `Flow`-Frame sagt
  dem Server nicht "gib mir `credit` mehr", sondern "setze mein Budget auf
  genau `credit`" — der Server übernimmt den Wert 1:1 (abzüglich seiner
  eigenen konfigurierten Obergrenze, `mq:dispatch:MaxCredit`), er addiert ihn
  nicht auf das bestehende Budget. Das macht ein verlorenes oder dupliziertes
  `Flow`-Frame ungefährlich (der jeweils letzte gültige Wert gewinnt, egal wie
  oft er ankommt) und erlaubt es dem Client, das Fenster in beide Richtungen
  zu verändern: ein größeres `prefetch` grantet sofort mehr Budget, ein
  kleineres senkt die Obergrenze sofort — beides ohne auf den nächsten
  Ack-Batch warten zu müssen (siehe [10-client.md](10-client.md)). Da `credit`
  im Protokoll ein `uint32` ist, ist es immer nicht-negativ; ein Wert *unter*
  dem aktuell unbestätigten Rückstau senkt das Budget effektiv auf `0`, ohne
  dass bereits ausgelieferte `Deliver`-Frames zurückgenommen werden könnten —
  das kann kein Flow-Wert, positiv oder negativ, da eine Zustellung nicht
  "entsendet" werden kann.
- Der Server dekrementiert das aktuell gesetzte Credit-Budget pro
  ausgeliefertem `Deliver` und stoppt die Zustellung auf diesem Link bei
  Budget `0`, bis neues `Flow` eintrifft — verhindert, dass ein langsamer
  Consumer überflutet wird, ohne auf HTTP/2-Flow-Control-Frames angewiesen zu
  sein (siehe [01-transport.md](01-transport.md)).
- **Credit misst Zustellungen, nicht Erfolge: ein `Ack` gibt kein Credit
  zurück** — unabhängig vom `Outcome`. Eine mit `REJECTED` abgelehnte oder mit
  `RELEASED` zurückgegebene Nachricht kostet exakt so viel Budget wie eine
  erfolgreich verarbeitete. Das ist so gewollt: Credit ist die Zusage des
  Consumers, wie viele Nachrichten er *entgegennehmen* kann, und diese Zusage
  ist mit der Zustellung eingelöst. Würde der Server bei `RELEASED`
  nachfüllen, könnte eine Nachricht, die der Consumer dauerhaft nicht
  verarbeiten kann, sich selbst unbegrenzt oft neu finanzieren und die
  Rückstau-Wirkung des Budgets aushebeln. Nachschub ist deshalb
  ausschließlich eine Client-Entscheidung per `Flow`.
- Daraus folgt: **der Client führt das Budget selbst mit** — er zählt
  Zustellungen minus Bestätigungen (`ausstehend`) und schickt, sobald genug
  Bestätigungen seit dem letzten `Flow` zusammengekommen sind, ein neues
  `Flow(credit = prefetch − ausstehend)`. Server→Client-`Flow` ist rein
  informativ und darf ignoriert werden. Der .NET-Client vergibt bei `Attach`
  einmal `Flow(credit = prefetch)` (mit `ausstehend = 0` ist das genau
  `prefetch`) und schickt beim Durchiterieren ein neu berechnetes, absolutes
  `Flow` heraus, sobald die Hälfte des Fensters seit dem letzten Nachschub
  bestätigt wurde — ein `Flow`-Frame pro Batch statt pro Nachricht (siehe
  [10-client.md](10-client.md)).

### Ping/Pong — Liveness

```protobuf
message Ping {
  uint64 nonce           = 1;  // opakes Korrelations-Token, verbatim zu echoen
  int64  sent_at_unix_ms = 2;  // Uhr des Senders, verbatim zu echoen
}

message Pong {
  uint64 nonce           = 1;
  int64  sent_at_unix_ms = 2;
}
```

**Symmetrisch**: beide Seiten dürfen `Ping` senden, und der Empfänger **muss**
mit einem `Pong` antworten, das beide Felder **unverändert** zurückgibt. Nur der
Absender interpretiert sie — er misst damit die Round-Trip-Zeit gegen seine
eigene Uhr. Ein Vergleich über Maschinengrenzen hinweg findet nicht statt, die
Uhren müssen also nicht synchron sein. `Ping` ist vor `Open` erlaubt: es beweist
Liveness und berührt keinen Zustand.

**Semantik auf Broker-Seite** (`mq:heartbeat`, `Services/HeartbeatOptions.cs`):

- **Jeder** eingehende Frame gilt als Liveness-Beweis, nicht nur ein `Pong`.
  Eine Verbindung mit Verkehr wird deshalb nie gepingt.
- War eine Verbindung `interval` lang still, sendet der Broker ein `Ping`.
  Kommt danach `timeout` lang **kein Frame** — auch kein anderer als das
  erwartete `Pong` —, gilt der Peer als tot und die Verbindung wird abgebrochen.
- Der Abbruch nimmt exakt den Weg, den ein Transport-Abbruch nähme: die
  bestehende Teardown-Logik detacht Consumer, gibt Credit frei und wendet den
  Transient-Queue-Lifecycle an. Der Heartbeat liefert nur den fehlenden Auslöser.
- `interval: "00:00:00"` schaltet den Mechanismus ab (reines Transport-Verhalten).

**Warum das im Protokoll steht und nicht dem Transport überlassen bleibt**: Kein
Transport-Mechanismus deckt alle Fälle ab. Kestrels HTTP/2-Keepalive-Ping ist
per Default aus und gilt nicht für HTTP/3; QUICs eigener Idle-Timeout greift
zwar, ist über Kestrel aber nicht konfigurierbar (`QuicTransportOptions`
exponiert weder `IdleTimeout` noch `KeepAliveInterval`). Ein Heartbeat im
Protokoll gibt beiden HTTP-Versionen dieselbe, einstellbare Deadline — und
erkennt zusätzlich den Fall, den kein Transport-Check sehen kann: einen Peer,
dessen Netzwerk-Stack noch antwortet, während die Anwendung darüber hängt.

**Aushandlung**: `Open.heartbeat_ms` trägt einen **Vorschlag**, `Opened.heartbeat_ms`
die Antwort — das tatsächlich geltende Intervall. Der Client schlägt vor, der
Broker entscheidet:

| Vorschlag | Ergebnis |
|---|---|
| nicht gesetzt oder `0` | der konfigurierte Default des Brokers |
| unter `minNegotiableInterval` | auf die Untergrenze angehoben |
| über `maxNegotiableInterval` | auf die Obergrenze gesenkt |
| dazwischen | wird übernommen |

Ein Vorschlag wird also **geklemmt, nicht abgelehnt** — eine Verbindung ist es
nicht wert, an einer Zahl zu scheitern, die der Broker ebenso gut korrigieren
kann. `Opened` trägt die Antwort immer, damit ein Client nachlesen kann, was er
bekommen hat, statt seinen Vorschlag anzunehmen.

Was ein Client **nicht** kann, ist den Heartbeat abschalten. `0` heißt „keine
Präferenz", nicht „lass mich in Ruhe". Ob Liveness durchgesetzt wird, ist eine
Betreiberentscheidung (`mq:heartbeat:interval`), denn die Kosten einer
Verbindung, an deren anderem Ende niemand mehr ist, trägt der Broker: ein
Geister-Consumer hält sein Prefetch-Credit und seinen Exclusive-Anspruch. Aus
demselben Grund sind die Grenzen überhaupt da — ohne Untergrenze könnte ein
Client den Broker beliebig oft pingen lassen, ohne Obergrenze Ressourcen
beliebig lange blockieren.

**Erwartung an Fremd-Clients**: Wer `Ping` ignoriert, wird nach
`interval + timeout` getrennt — auch wer das Feld gar nicht kennt. Das ist eine
bewusste Entscheidung und keine Lücke: Der Alternativvorschlag, die Durchsetzung
für nicht-meldende Clients auszusetzen, hätte genau für die Verbindungen keine
Geistererkennung mehr geliefert, bei denen sie am ehesten fehlt.

## Ablaufbeispiele

### Producer publiziert (Queue-Modus)

```
Client                                   Server
  │── Open(vhost) ─────────────────────▶ │
  │◀──────────────────────── Opened ─────│
  │── Attach(PRODUCER, exchange) ──────▶ │
  │◀──────────────────────── Attached ───│
  │── Publish(msg) ─────────────────────▶│  Leader hängt an FIFO-Log an,
  │                                       │  repliziert (siehe 03-replication.md)
  │◀── (kein Error-Frame = angenommen) ──│  Commit nach Mehrheits-Ack
```

### Consumer konsumiert mit Ack (Queue-Modus)

```
Client                                   Server
  │── Attach(CONSUMER, queue) ─────────▶ │
  │◀──────────────────────── Attached ───│
  │── Flow(credit=10) ──────────────────▶│
  │◀── Deliver(tag=1) ────────────────── │
  │◀── Deliver(tag=2) ────────────────── │
  │── Ack(tag=1, ACCEPTED) ─────────────▶│
  │── Ack(tag=2, ACCEPTED) ─────────────▶│
  │── Flow(credit=10) ──────────────────▶│  (Nachschub, wenn Budget aufgebraucht)
```

Bleibt ein `Ack` innerhalb des Redelivery-Timeouts aus, sendet der Server
`Deliver` mit `redelivered = true` erneut (siehe Konsistenzgarantien,
[01-transport.md](01-transport.md)).

### Broadcast (Fire-and-forget)

```
Client                                   Server
  │── Attach(PRODUCER, exchange, mode=BROADCAST) ▶│
  │◀──────────────────────── Attached ────────────│
  │── Publish(msg) ──────────────────────────────▶│  kein Log-Write, direkt an
  │                                                │  aktive Consumer-Streams
```

## Implementierungsprinzipien (bindend, siehe 07-implementation-principles.md)

- `oneof`-Dispatch auf `IAsyncStreamReader<ClientFrame>`/
  `IServerStreamWriter<ServerFrame>` direkt, kein Zwischenpuffern ganzer
  Frame-Batches.
- `Message.payload` bleibt unter dem 1-MB-Limit; größere Payloads nutzen
  ausschließlich den `ms-link`-Header, nie Chunking über mehrere `Publish`-
  Frames (kein Reassembly-Zustand pro Link nötig).
- Ein `Channel<T>`-Worker pro angehängtem Consumer-Link (siehe
  [09-worker-model.md](09-worker-model.md)) verarbeitet `Flow`/`Deliver` für
  diesen Link, damit mehrere Links auf demselben Stream sich nicht
  gegenseitig blockieren.
- **Ein Writer-Task pro Stream, kein Write unter einem Lock**: alle
  Frame-Produzenten eines Streams (Control-Loop, Consumer-Link-Worker)
  legen ihre Frames in einen **bounded** `Channel<T>` ab; genau ein Task
  drainiert ihn auf den gRPC-Stream. Ein Write, der auf HTTP/2-Flow-Control
  wartet, hält so keinen Lock mehr, hinter dem alle anderen Produzenten
  desselben Streams stehen — die Backpressure ist das Vollaufen des Channels.
  Konsequenz: „eingereiht" heißt nicht mehr „auf dem Draht"; ein
  fehlgeschlagener Write beendet den ganzen Stream (Abbruch der Read-Loop),
  statt beim einreihenden Aufrufer aufzuschlagen. Unbounded wäre falsch:
  das verschöbe die Backpressure nur in den Heap.
- **Client-Topologie: ein Stream pro Link.** Der .NET-Client nutzt die oben
  erlaubte Mehr-Stream-Option als Standard: jeder Producer-/Consumer-Link
  öffnet seinen eigenen `Connect`-Stream (Open+Attach werden gepipelined
  gesendet, bevor auf die Antworten gewartet wird — kein zusätzlicher
  Roundtrip), plus ein link-freier Control-Stream für den
  Verbindungs-Handshake. Jeder Link hat damit seinen eigenen Writer und sein
  eigenes HTTP/2-Flow-Control-Fenster: ein langsamer Consumer staut nur den
  eigenen Stream, nie den eines Geschwister-Links. Serverseitig ist das kein
  Sonderfall — ein Stream mit genau einem Link ist das normale
  Multi-Link-Protokoll.
- **`MAX_CONCURRENT_STREAMS` ist damit die Obergrenze für Links pro Verbindung.**
  Ein Link-Stream bleibt für die Lebensdauer des Links offen, zählt also dauerhaft
  gegen das Limit. Serverseitig ist es über `mq:grpc:maxConcurrentStreams`
  einstellbar (Kestrel-Default 100). Clients müssen damit rechnen, dass ein
  Überschreiten **nicht** als Fehler zurückkommt, sondern den Aufruf im Client
  blockiert — die .NET-Implementierung begegnet dem mit zusätzlichen
  HTTP/2-Verbindungen (`EnableMultipleHttp2Connections`, per Default an). Ein
  Client, der viele Links auf einer Verbindung halten will, muss entweder das
  Serverlimit anheben oder mehrere Verbindungen zulassen.

## Offene Fragen

- Mehrere gleichzeitige Consumer-Links auf einem Stream: Priorisierung der
  `Deliver`-Frames verschiedener `link_id`s im gemeinsamen Writer-Channel
  (Fairness zwischen Links) noch nicht spezifiziert. Durch die
  Ein-Stream-pro-Link-Topologie des .NET-Clients tritt der Fall dort nicht
  auf; er betrifft nur Fremd-Clients, die mehrere Links auf einen Stream
  multiplexen.
- ~~Heartbeat/Keepalive-Frame (analog AMQP `empty frame`) für Verbindungs-
  Liveness-Checks jenseits von HTTP/2-Pings noch nicht entschieden.~~
  **Entschieden und implementiert**, siehe Abschnitt „Ping/Pong — Liveness".
  Offen bleibt daraus nur die Aushandlung mit Fremd-Clients: ein Client, der
  `Ping` nicht beantwortet, wird getrennt, ohne dass es einen Weg gäbe, den
  Heartbeat pro Verbindung abzuwählen oder ein Intervall zu vereinbaren
  (`Open.properties` wäre der naheliegende Ort).
- Client-seitige Liveness-Prüfung des Brokers: `Ping` ist symmetrisch
  spezifiziert und der Broker antwortet darauf, aber der .NET-Client sendet
  selbst keine — er verlässt sich auf `SocketsHttpHandler`s
  Keepalive-Ping, und der gilt nur für HTTP/2. Über HTTP/3 erkennt der Client
  einen toten Broker daher nicht zeitnah (siehe ISSUES #12).
- Reconnect-Semantik bei Leader-Failover (Abschnitt 3, 10-client.md):
  müssen `link_id`s nach Reconnect erneut `Attach`-t werden, oder wird
  Link-Zustand server-seitig kurzzeitig vorgehalten?
