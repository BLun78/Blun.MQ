# Spec: Exchanges (Routing)

> Quelle: [Vision.md](../Vision.md), Abschnitt 1a

## Scope

Vier Exchange-Typen, an die Producer publizieren; Exchange routet an gebundene
Queues (Consumer- und Sync-Queues). Bindings sind pro vhost definiert (siehe
[02-sharding.md](02-sharding.md)) und existieren nicht vhost-übergreifend.

## Exchange-Typen

### Direct

- Exakte Übereinstimmung Routing-Key ↔ Binding-Key.
- 1-zu-1-Zuordnung, gezielte Aufgabenverteilung.

### Fanout

- Ignoriert Routing-Key vollständig.
- Kopiert Nachricht an **alle** gebundenen Queues.
- Entspricht klassischem Broadcast/Pub-Sub (siehe
  [01-transport.md](01-transport.md), Betriebsmodus "Broadcast").

### Topic

- Routing-Key aus mehreren durch Punkte getrennten Wörtern
  (z.B. `europa.deutschland.berlin`).
- Wildcards: `*` = genau ein Wort, `#` = null oder mehr Wörter.
- Anwendungsfall: Log-Routing (`#.error`, `order-service.database.critical`).
- **Semantik ist die von RabbitMQ**, absichtlich bis in die Randfälle
  ([Tutorial 5](https://www.rabbitmq.com/tutorials/tutorial-five-python#topic-exchange),
  Tabelle als Testfälle in `src/test/Engine/TopicPatternTests.cs`):
  - Ein Wildcard ist nur als **ganzes Wort** ein Wildcard — `a*` ist das
    Literal `a*`.
  - Der Key wird auf `.` **gesplittet**, ein leeres Segment ist ein Wort:
    `a.` sind die zwei Wörter `a` und `""`, der leere String ist null Wörter.
    Deshalb matcht `*` keinen leeren Routing-Key, `a.*` aber sehr wohl `a.`.
  - `#` allein matcht alles (wie Fanout), ein Pattern ohne Wildcards verhält
    sich wie Direct.
- **Länge: max. 255 UTF8-Bytes** für Routing-Key *und* Pattern
  (`ExchangeBinding.MaxRoutingKeyLength`), wie bei RabbitMQ. Das ist keine
  Plausibilitätsgrenze: der Match kostet Pattern-Wörter × Key-Wörter, beide
  Seiten kommen vom Client, und ohne Deckel ist das ein DoS auf dem
  Publish-Pfad. Verletzung → `INVALID_ARGUMENT` beim Binden bzw.
  `ROUTING_KEY_TOO_LONG` beim Publish.

### Headers

- Ignoriert Routing-Key; Routing über Nachrichten-Header/Metadaten.
- Binding legt erforderliche Header-Attribute fest, gesteuert über
  `he-match: all` (alle müssen matchen) oder `he-match: any` (mind. einer).
- Langsamer als Topic, erlaubt aber Routing über komplexe Attribute
  (z.B. `format: pdf`, `type: invoice`).

## Anforderungen

- Alle vier Typen routen sowohl an Consumer- als auch an Sync-Queues.
- Bindings sind strikt vhost-scoped.

## Umsetzung

Implementiert in `Engine/Exchanges/` — `ExchangeBase.cs` (Bindings, Zähler, der
Publish-Einstieg `Route`) mit je einer Klasse pro Typ (`DirectExchange`,
`FanoutExchange`, `TopicExchange`, `HeadersExchange`, erzeugt über
`ExchangeBase.Create`) und `TopicPattern` für `*`/`#` —,
`Engine/MessageBroker.cs` (Exchange-Tabelle pro Vhost) und
`Services/MessageQueueService.cs` (Publish-Pfad). Die wichtigsten Festlegungen:

- **Explizite Deklaration.** Wie Queues (12-declaration.md): `DeclareExchange` /
  `DeleteExchange` / `ListExchanges` / `BindQueue` / `UnbindQueue` auf
  `MessageQueueAdmin`, mit denselben `DeclareMode`-Regeln. Ein Attach oder
  Publish auf einen nicht deklarierten Exchange schlägt mit
  `EXCHANGE_NOT_FOUND` fehl. Der Typ ist bei der Deklaration fixiert; ein
  abweichender Re-Declare ist ein Konflikt.
- **Default-Exchange.** Ein leerer `Attach.exchange` ist der namenlose
  Default-Exchange: `routing_key` ist dann der Ziel-Queue-Name, zugestellt
  direkt — das Verhalten von vor den Exchanges, als gewöhnlicher Fall
  erhalten. Der leere Name kann nicht deklariert, gelöscht oder gebunden
  werden.
- **Routing-Key pro Nachricht.** `Message.routing_key` übersteuert den
  Link-Default aus `Attach.routing_key`. Ohne das bräuchte ein Topic-Exchange
  einen Producer-Link (= gRPC-Stream) pro Key.
- **Unroutable = verworfen.** Matcht kein Binding, wird die Nachricht
  verworfen (Debug-Log `Unroutable`), nicht als Fehler gemeldet; ein
  bestätigender Publish bekommt `PUBLISH_ACCEPTED`. Bei mehreren Ziel-Queues
  ist `PUBLISH_DUPLICATE` nicht mehr eindeutig und wird nur auf dem
  Default-Exchange gemeldet.
- **Binding-Form wird geprüft.** Routing-Key für Direct/Topic, Header für
  Headers (mindestens eines), nichts für Fanout, und in keinem Fall ein
  Routing-Key über 255 UTF8-Bytes — alles andere ist `INVALID_ARGUMENT`. Ein
  Headers-Binding ohne Attribute matcht nichts (nicht alles). Eine Queue, die
  über mehrere Bindings matcht, erhält eine Kopie.
- **Eine Klasse pro Typ, Arbeit auf den Bind-Pfad verschoben.** Der Typ steht
  bei der Deklaration fest, also entscheidet ihn die Klasse und nicht ein
  `switch` pro Nachricht. Jeder Typ übersetzt seine Bindings beim Bind/Unbind
  in genau die Form, gegen die er routet, und tauscht sie als ein
  unveränderliches Objekt per Compare-Exchange: Fanout hält die fertige
  Zielliste, Direct einen Index Routing-Key → Ziele, Topic denselben Index für
  wildcard-freie Patterns plus die restlichen Bindings zum Durchlaufen, Headers
  nichts (der Vergleich hängt an der Nachricht). Ein Publish auf Direct oder
  auf ein rein literales Topic ist damit ein Hash-Lookup statt eines Scans und
  allokiert nichts — die vorberechneten Arrays sind zugleich die Antwort und
  dürfen vom Aufrufer nicht verändert werden (`ExchangeRoutingBenchmarks`).
- **Persistenz.** Exchanges und Bindings sind Konfiguration und liegen in
  LiteDB (`ExchangeDocument`, Restore in `VhostRegistry`) — nicht im
  FasterLog-WAL, das Nachrichtenströmen vorbehalten bleibt (Vision Abschnitt 8).
- **Queue-Lebenszyklus.** Der ausdrückliche `DeleteQueue` eines Clients
  entfernt auch alle Bindings auf diese Queue; ein serverseitiger Abbau
  (transiente Queue, TTL, Auto-Delete) lässt sie stehen, weil die Queue beim
  Reconnect neu deklariert wird und ihre Topologie wiederfinden muss. Ein
  Binding auf eine gerade nicht existierende Queue routet ins Leere.
- **Envelope-Provenienz.** `MessageEnvelope.Exchange`/`RoutingKey` werden auf
  dem direkten Enqueue-Pfad befüllt (Admin-/DLQ-Forensik); der Raft- und der
  SMQ-Umweg verlieren sie derzeit — bekannte Lücke, hängt an ISSUES #2/#5.

## Metriken

Pro Exchange, getaggt mit `blun.mq.vhost`, `blun.mq.exchange` und
`blun.mq.exchange.type` (`MqMetrics`, Meter `Blun.MQ`):

| Instrument | Typ | Bedeutung |
| --- | --- | --- |
| `blun.mq.exchange.messages.published` | Counter | Nachrichten, die der Exchange routen sollte — eine pro Publish, unabhängig vom Treffer. |
| `blun.mq.exchange.messages.routed` | Counter | Erzeugte Queue-Kopien; ein Publish auf drei Bindings zählt drei. Gegen `published` ist das der Fan-out-Faktor. |
| `blun.mq.exchange.messages.unroutable` | Counter | Nachrichten, die kein Binding traf und die deshalb verworfen wurden. Steigt das gegen ein flaches `routed`, stimmt ein Binding-Pattern nicht. |
| `blun.mq.exchange.bindings` | Gauge | Aktuell gebundene Queues. |

Wie bei den Queue-Metriken sind das kumulative Zähler, keine Raten — die
Ableitung gehört ins Backend (`rate(...)`). Die Zähler sind In-Memory und
knotenlokal, beginnen also nach einem Neustart wieder bei null. Gezählt wird in
`ExchangeBase.Route()`, weil nur dort bekannt ist, was ein Publish tatsächlich
getroffen hat.

Der Default-Exchange taucht nie auf: er ist die Abwesenheit eines Exchanges, und
sein Verkehr ist bereits vollständig über die Zähler der Ziel-Queue beschrieben.

Die Exchanges-Seite der Admin-UI zeigt dieselben Zahlen (Rate aus zwei
aufeinanderfolgenden Samples, Summe darunter) und abonniert dafür den
Live-Push-Topic `exchanges:{vhost}`.
