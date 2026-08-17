# Vision

Ein Message-Queuing-System auf Basis von HTTP/2-Streams (.NET), das Broadcast- und
Queue-Semantik einfach zugänglich macht. Ziel ist ein horizontal skalierendes SaaS mit
Sharding über virtuelle Hosts, Raft/Paxos-basierter Replikation der Queue-Daten und
Userverwaltung über OpenID Connect.

## Architekturüberblick

```text
                     ┌─────────────────────────┐
                     │   Client (Producer /     │
                     │   Consumer)              │
                     └───────────┬─────────────┘
                                 │ HTTP/2 Stream
                     ┌───────────▼─────────────┐
                     │   Gateway / Edge Node     │
                     │  - OIDC Token-Validierung │
                     │  - Routing zu Shard       │
                     │  - Connection-Multiplexing│
                     └───────────┬─────────────┘
                                 │
                 ┌───────────────┼───────────────┐
                 │               │               │
           ┌─────▼─────┐   ┌─────▼─────┐   ┌─────▼─────┐
           │  Shard A   │   │  Shard B   │   │  Shard C   │
           │ (Virtual   │   │ (Virtual   │   │ (Virtual   │
           │  Host)     │   │  Host)     │   │  Host)     │
           │            │   │            │   │            │
           │ Raft-Group │   │ Raft-Group │   │ Raft-Group │
           │ (Leader +  │   │ (Leader +  │   │ (Leader +  │
           │  Follower) │   │  Follower) │   │  Follower) │
           └─────┬─────┘   └───────────┘   └───────────┘
                 │
           ┌─────▼─────┐
           │ FIFO-Log   │
           │ (append-only,
           │  segmentiert)
           └───────────┘
```

## 1. Transport-Schicht (HTTP/2 Streams)

- Jede Verbindung nutzt HTTP/2-Multiplexing: ein Stream pro Queue-Subscription bzw.
  Broadcast-Kanal, mehrere Streams über eine TCP-Verbindung.
- Das Protokoll wird mit **gRPC** (bidirektionales Streaming über HTTP/2) und
  **Protocol Buffers** als Nachrichtenformat umgesetzt — kein eigenes Framing-Format,
  sondern die von gRPC bereitgestellten Streaming-Contracts (`.proto`-Definitionen) für:
  - Publish (Producer → Server)
  - Deliver + Ack/Nack (Server → Consumer, Consumer → Server)
  - Flow-Control (Credit-based, um Consumer nicht zu überfluten; über gRPC-Nachrichten
    statt HTTP/2-Flow-Control-Frames direkt gesteuert)
- Vorteil ggü. eigenem Binärprotokoll: generierte Client-/Server-Stubs für .NET (und
  perspektivisch weitere Sprachen), Schema-Evolution über Protobuf-Versionierung,
  geringerer Implementierungsaufwand für Framing/Serialisierung.
- Bleibt konsistent mit Abschnitt 7 (Streams-only): gRPC-Streaming-APIs in .NET arbeiten
  bereits auf `IAsyncStreamReader`/`IServerStreamWriter` ohne vollständige Message-
  Materialisierung im Speicher.
- **Maximale Nachrichtengröße: 1 MB** (Payload-Limit, engine-seitig durchgesetzt). Größere
  Payloads werden nicht direkt transportiert, sondern per Header referenziert:
  - Header **`ms-link`**: `{uri/string to source}` — verweist auf den tatsächlichen
    Speicherort der (größeren) Payload außerhalb der Engine (z.B. Blob-Storage-URI).
  - Die Nachricht selbst enthält dann keinen (oder nur einen Teil-)Payload, sondern nur
    Metadaten inkl. `ms-link`; Consumer lösen den Verweis eigenständig auf.
  - Damit bleibt das FIFO-Log (Abschnitt 4) auf kleine, vorhersagbare Eintragsgrößen
    begrenzt — wichtig für Replikationsdurchsatz (Abschnitt 3) und Speicher-/I/O-Planung.
- Zwei Betriebsmodi pro Stream:
  - **Broadcast**: Fire-and-forget, kein Ack, kein Replay (Pub/Sub-Charakter).
  - **Queue**: At-least-once mit Ack, Redelivery bei Timeout, Consumer-Groups für
    Lastverteilung (competing consumers).

### Konsistenzgarantien: Broadcast vs. Queue

- **Broadcast (best-effort)**:
  - Keine Zustellgarantie über den Moment des Publish hinaus — ist beim Publish kein
    Consumer verbunden, geht die Nachricht verloren, es gibt kein Replay und keine
    Persistenz im FIFO-Log (Abschnitt 4).
  - Der Server bestätigt dem Producer nur die **Annahme** der Nachricht (Publish wurde
    entgegengenommen), nicht deren tatsächliche Zustellung an einen oder mehrere
    Consumer.
  - Kein Ordering-Anspruch über mehrere Consumer hinweg; jeder Consumer erhält die
    Nachrichten in Sendereihenfolge, aber ohne Zustellbestätigung an den Server.
- **Queue (at-least-once)**:
  - Eine Nachricht gilt als **zugestellt**, sobald sie beim Leader der vhost-Raft-Gruppe
    committed ist (Mehrheits-Ack, siehe Abschnitt 3) — das ist die Persistenzgarantie,
    unabhängig vom Consumer-Ack.
  - Zusätzlich erwartet der Server ein **Consumer-Ack nach Verarbeitung**; bleibt dieses
    innerhalb eines konfigurierbaren Redelivery-Timeouts aus (z.B. Consumer-Crash,
    Verbindungsabbruch), wird die Nachricht erneut zugestellt (Redelivery).
  - Redelivery kann zu **Duplikaten** führen — die Garantie ist at-least-once, nicht
    exactly-once. Konsumenten müssen Verarbeitung idempotent gestalten, falls das
    relevant ist.
  - Nach x fehlgeschlagenen Redelivery-Versuchen wandert die Nachricht in die DLQ
    (Abschnitt 4d) statt unbegrenzt weiter zugestellt zu werden.

## 1a. Exchanges (Routing)

Producer publizieren nicht direkt in eine Queue, sondern an einen **Exchange**, der die
Nachricht anhand seines Typs an eine oder mehrere gebundene Queues weiterleitet. Es gibt
vier Standard-Exchange-Typen:

### Direct Exchange

- Exakte Übereinstimmung des Routing-Keys (z.B. `pdf.create`) mit dem Binding-Key der Queue.
- Sehr schnell, präzise 1-zu-1-Zuordnung.
- Anwendungsfall: gezielte Aufgabenverteilung (z.B. Bildverarbeitungsauftrag an eine
  spezielle Queue).

### Fanout Exchange

- Ignoriert den Routing Key vollständig; die Nachricht wird an **alle** gebundenen Queues
  kopiert und zugestellt.
- Entspricht dem klassischen Broadcast/Pub-Sub-Muster (siehe Abschnitt 1, Betriebsmodus
  "Broadcast").
- Anwendungsfall: systemweite Benachrichtigungen an mehrere unabhängige Queues gleichzeitig
  (z.B. Push-Notification-, Live-Stats- und Frontend-Queue bei einem Ereignis).

### Topic Exchange

- Routing Key aus mehreren durch Punkte getrennten Wörtern (z.B. `europa.deutschland.berlin`).
- Wildcard-Bindings: `*` steht für genau ein Wort, `#` für null oder mehr Wörter.
- Flexibel für komplexe Routing-Szenarien.
- Anwendungsfall: Log-Systeme, z.B. eine Queue für alle Fehler (`#.error`), eine andere nur
  für kritische DB-Fehler (`order-service.database.critical`).

### Headers Exchange

- Ignoriert den Routing Key; Routing erfolgt über Nachrichten-Header/Metadaten.
- Beim Binden wird festgelegt, welche Header-Attribute vorhanden sein müssen; gesteuert
  über `he-match: all` (alle Header müssen übereinstimmen) oder `he-match: any` (mindestens
  einer).
- Etwas langsamer als Topic Exchange, erlaubt aber Routing anhand komplexerer Attribute
  ohne Kodierung im Routing Key.
- Anwendungsfall: Routing nach Dateityp (`format: pdf`, `type: invoice`) oder Rolle.

Alle vier Exchange-Typen routen sowohl an Consumer- als auch an Sync Queues (Abschnitt 4a/4b)
und sind pro vhost definiert (Abschnitt 2) — Bindings existieren also nur innerhalb des
jeweiligen vhosts, nicht vhost-übergreifend.

## 2. Sharding über virtuelle Hosts

- Jeder Tenant (oder Gruppe von Queues) bekommt einen **virtuellen Host** (vhost) als
  Sharding-Key — analog RabbitMQ-vhosts, aber hier 1:1 einem Shard zugeordnet.
- Der Gateway/Edge-Node löst beim Connect (per Hostname/Header/Pfad) den vhost auf und
  routet den Stream direkt zum zuständigen Shard.
- Vorteil ggü. Hash-Sharding auf Message-Key: Tenant-Daten bleiben zusammen (Rebalancing,
  Backups, Compliance/Datenresidenz pro Kunde einfacher), Routing-Entscheidung ist
  billig (Lookup statt Hash über Payload).
- Für sehr große Tenants: vhost kann selbst wieder in mehrere Shards unterteilt werden
  (Sub-Sharding), falls ein einzelner Shard zum Bottleneck wird.
- Ein Verzeichnisdienst (z.B. eigene Konsensus-gestützte Shard-Map oder etcd/Raft-basiertes
  Metadaten-Log) hält die Zuordnung vhost → Shard/Node vor.

## 2a. Sub-Sharding und Rebalancing

- **Trigger**: Ein zentraler Cluster-Koordinator beobachtet pro vhost Metriken wie
  Durchsatz, FIFO-Log-Größe, Anzahl Queues und Leader-CPU-Last (siehe Leader-Count-
  Balancing, Abschnitt 3). Überschreitet ein vhost definierte Schwellwerte, wird er in
  mehrere Sub-Shards aufgeteilt — jeweils eigene Raft-Gruppe innerhalb desselben vhosts,
  Aufteilung z.B. nach Hash der Queue-Namen.
- **Rebalancing-Mechanismus: Snapshot-basiertes Raft-Membership-Change.** Beim
  Verschieben eines (Sub-)Shards auf einen anderen Node läuft kein "Stop-the-world"-
  Kopiervorgang, sondern:
  1. Der neue Ziel-Node wird der bestehenden Raft-Gruppe als zusätzlicher, nicht-
     stimmberechtigter Follower hinzugefügt (Membership-Change).
  2. Er lädt zunächst einen **Snapshot** des aktuellen FIFO-Logs (Abschnitt 4) und holt
     danach über normale Log-Replikation die seit dem Snapshot committeten Einträge auf.
  3. Sobald er caught-up ist, wird er zum vollwertigen, stimmberechtigten Mitglied
     befördert; erst danach wird der alte Node aus der Raft-Gruppe entfernt.
  - Vorteil: kein Downtime-Fenster, keine Inkonsistenz während der Migration — die
    Raft-Gruppe bleibt durchgehend schreib-/lesefähig, da die Mehrheit unverändert bleibt.
- **Koordination**: Der zentrale Koordinator triggert und orchestriert den Ablauf und
  aktualisiert nach Abschluss die Shard-Map (Verzeichnisdienst, s.o.); Gateways lesen die
  Shard-Map bei jedem Connect/Reconnect, sodass Publishes während der Migration bestenfalls
  kurzzeitig auf die alte Adresse treffen und per Redirect/Retry (analog Failover,
  Abschnitt 3) auf die neue Zuordnung umgeleitet werden.
- **Trade-off der Koordinator-Wahl**: ein zentraler Koordinator ist einfacher zu bauen und
  zu debuggen als ein vollständig dezentrales, selbstorganisierendes Rebalancing, bildet
  aber einen zusätzlichen Komplexitätsträger (muss selbst hochverfügbar ausgelegt werden,
  z.B. ebenfalls über eine kleine Raft-Gruppe).

## 3. Replikation (Raft/Paxos)

- Jeder Shard (vhost) läuft als eigene **Raft-Gruppe** (empfohlen ggü. Paxos: einfacher zu
  implementieren und zu betreiben, gute .NET-Bibliothekslage z.B. `dotnext.net.cluster`).
- Pro vhost: 1 Leader + N Follower (typisch 3 oder 5 Knoten für Fehlertoleranz).
- **Wichtig: Leader-Election passiert pro vhost, nicht pro physischem Node.** Ein
  physischer Node hostet mehrere vhosts gleichzeitig und ist für manche davon Leader,
  für andere Follower. Dadurch verteilt sich die CPU-Last der Leader-Rolle (Log-Writes,
  Replikations-Koordination, Client-Serving) gleichmäßig über den Cluster statt sich auf
  wenige "Leader-Nodes" zu konzentrieren.
- Platzierung/Rebalancing der vhost-Raft-Gruppen berücksichtigt aktive Leader-Zahl pro
  Node als Kriterium (Leader-Count-Balancing), nicht nur Datenvolumen oder Anzahl vhosts.
- Bei Node-Ausfall verteilen sich dessen Leader-Rollen (für die dort gehosteten vhosts)
  auf verschiedene übrig gebliebene Nodes, statt dass ein einzelner Node die gesamte
  Last übernimmt.
- Schreibpfad: Producer publiziert → Leader hängt Eintrag an lokales FIFO-Log an →
  repliziert an Follower → Commit nach Mehrheits-Ack → Ack an Producer.
- Lesepfad: Consumer liest vom Leader (oder Follower mit Read-Index/Lease für
  Skalierung von Lesezugriffen, falls nötig).
- Failover: Bei Leader-Ausfall wählt die Raft-Gruppe automatisch neuen Leader; Clients
  erkennen das über Redirect/Retry mit aktueller Leader-Adresse aus der Shard-Map.

## 4. FIFO-Speicherformat

- Append-only, segmentiertes Log pro Queue (ähnlich Kafka-Segmentdateien):
  - feste Segmentgröße, Rotation bei Limit
  - sequenzielle Writes (schnell, kein Random I/O)
  - Index-Datei pro Segment (Offset → Dateiposition) für O(1)-Lookup
- Jeder Log-Eintrag: Offset, Timestamp, Message-ID, Payload, optional Header/Metadata.
- Konsum-Position (Consumer-Offset) wird getrennt vom Log gespeichert, pro
  angehängtem Consumer-Link (nicht pro Queue und nicht pro Consumer-Group —
  siehe `doc/specs/04e-wal-fasterlog.md`, Abschnitt "Ack-Offset- und
  Dedup-WAL"), damit mehrere Consumer dieselbe Queue mit unabhängigem
  Zustellfortschritt lesen können. `consumer_group` ist als Feld auf `Attach`
  vorgesehen, wird aber noch nicht ausgewertet (jeder Consumer auf einer
  Queue konkurriert unabhängig davon, siehe `doc/ISSUES.md` #5) —
  Offset-Sharing innerhalb einer Gruppe ist damit noch keine gebaute Semantik.
- Kompaktierung/Retention: zeit- oder größenbasiert, alte Segmente werden verworfen oder
  archiviert.
- Das Log selbst ist die Einheit, die per Raft repliziert wird (Log-Replikation und
  Consensus-Log fallen hier bewusst zusammen).

### Empfohlene .NET-Bibliothek

- **FasterLog** (aus Microsofts Tsavorite-Projekt, Nachfolger von FASTER; NuGet:
  `Tsavorite`/`Microsoft.FASTER.Core`) — speziell für hochperformante, persistente
  Append-only-Logs entwickelt: sequenzielle Writes, Iteratoren über Offsets, eingebaute
  Truncation/Commit-Semantik. Deckt den in diesem Abschnitt beschriebenen Log-Aufbau
  weitgehend ab, statt ihn von Grund auf selbst zu implementieren.
- Arbeitet mit `Span`/`Memory`-basierten APIs statt vollständiger Materialisierung —
  konsistent mit dem Streams-only-Prinzip (Abschnitt 7).
- **Trade-off**: FasterLog bringt kein Raft-/Replikations-Handling mit; die in Abschnitt 3
  beschriebene Replikation (Leader → Follower, Mehrheits-Commit) muss weiterhin selbst um
  FasterLog herum gebaut werden — die Bibliothek deckt nur die lokale Persistenzschicht ab.

### Speicherformat für Priority Queues

- Das FIFO-Log (append-only, sequenziell) bleibt unverändert die alleinige
  Persistenz-/Replikationsschicht — auch für Priority Queues wird ausschließlich
  sequenziell geschrieben, kein Random-I/O beim Publish.
- Zusätzlich existiert ein **separater Priority-Index**, umgesetzt als **Bucket-Ansatz**:
  pro Prioritätsstufe eine eigene `System.Collections.Concurrent.ConcurrentQueue<T>`
  (BCL, lock-free, keine externe Bibliothek nötig). Jede Bucket-Queue speichert nur
  Log-Offsets, keine Payload-Duplikate.
- Voraussetzung: begrenzte, diskrete Anzahl an Prioritätsstufen (z.B. 0–9) — für diesen
  Anwendungsfall bewusst gewählt statt eines kontinuierlichen Prioritätswerts mit Heap/
  Skip-List, da dadurch keine Heap-Contention entsteht und jede Bucket-Queue unabhängig
  lock-free arbeitet.
- Zustellung an Consumer erfolgt durch Iteration der Bucket-Queues von höchster zu
  niedrigster Prioritätsstufe; innerhalb einer Stufe bleibt die Einfüge-Reihenfolge
  (FIFO) erhalten, da `ConcurrentQueue<T>` selbst FIFO-Semantik hat. Der eigentliche
  Payload wird dann per Offset aus dem Log gelesen.
- **Sync Queues**: Der Priority-Index muss bei Leader-Wechsel/Neustart entweder
  mitrepliziert oder deterministisch aus dem Log rekonstruiert werden (Rebuild-Kosten
  skalieren mit Queue-Größe).
- **Consumer Queues**: Der Index lebt rein im Speicher (node-lokal, transient) —
  unproblematisch, da mit der Queue selbst beim Disconnect verworfen.

## 4a. Consumer Queues (transient, node-lokal)

- Existieren nur, solange eine aktive Verbindung zur Queue besteht.
- Keine Replikation über Raft/andere vhost-Nodes — rein node-lokal im Speicher.
- Ordnungssemantik wählbar: FIFO oder Priority Queue.
- Sobald die letzte Verbindung schließt, wird die Queue inkl. Inhalt verworfen.
- Anwendungsfall: kurzlebige Request/Response- oder Ephemeral-Subscriptions ohne
  Persistenzbedarf, günstiger (kein Log-Write, kein Konsens-Overhead).

## 4b. Sync Queues (persistent, über vhost-Nodes repliziert)

- Werden über die Raft-Gruppe des vhosts repliziert (siehe Abschnitt 3) und bleiben
  bestehen, auch ohne aktiven Consumer — Nachrichten werden durchgehend gespeichert.
- Nutzen das in Abschnitt 4 beschriebene FIFO-Log als Speicherformat.
- Ordnungssemantik wählbar: FIFO oder Priority Queue (Priority erfordert Index/Sortierung
  zusätzlich zum Append-only-Log, z.B. Skip-List/Heap-Index über die Log-Offsets).

## 4c. Delayed / Scheduled Messages

- Gilt für beide Queue-Typen und beide Ordnungssemantiken (FIFO/Priority).
- Publish mit Verzögerung, wahlweise angegeben als:
  - **TimeSpan** (relative Verzögerung, z.B. "in 10 Sekunden"), oder
  - **fester Zeitpunkt** (UTC oder lokale Zeit inkl. Zeitzone), zu dem die Nachricht in
    die Zielqueue verschoben werden soll.
- Die Nachricht wird sofort in einer eigenen, sichtbaren Queue abgelegt:
  **`{Target-Queue-Name}-SMQ`** (Scheduled Message-Queue) — automatisch von der Engine
  angelegt, analog zur DLQ-Namenskonvention (Abschnitt 4d).
- An eine SMQ kann **kein Consumer angehängt werden** — sie ist nicht konsumierbar.
  Einsehbar sind die enthaltenen Nachrichten (inkl. Fälligkeitszeitpunkt) ausschließlich
  über das Admin-Interface (Monitoring/Debugging, kein regulärer Zustellpfad).
- Nach Ablauf der Verzögerung bzw. Erreichen des Zielzeitpunkts verschiebt die Engine die
  Nachricht aus der SMQ in die eigentliche Zielqueue — ab dann regulär zustellbar wie jede
  andere Nachricht.
- Umsetzung: zeitindizierte Warteliste (Timer-Wheel oder sortierter Index nach
  Fälligkeitszeitpunkt) pro SMQ, die fällige Nachrichten in die Zielqueue verschiebt.
- Bei Sync Queues wird die SMQ ebenfalls über die Raft-Gruppe des vhosts repliziert, damit
  ein Leader-Wechsel das Scheduling nicht verliert. Bei Consumer Queues ist die SMQ analog
  transient/node-lokal.
- Bei Consumer Queues teilt die SMQ das Lebenszyklus-Schicksal der zugehörigen Consumer
  Queue: Trennt sich der Consumer (Disconnect), werden sowohl die Consumer Queue als auch
  ihre `-SMQ` samt aller noch nicht fälligen Nachrichten verworfen/gelöscht.

## 4d. Dead-Letter Queues (DLQ)

- Die Engine verschiebt Nachrichten automatisch in eine DLQ, wenn eine Zustellung nach
  x Redelivery-Versuchen fehlschlägt (x konfigurierbar, Default zu definieren).
- Namenskonvention: `{Source-Queue-Name}-DLQ` — automatisch von der Engine angelegt.
- Gilt für Consumer- wie Sync-Queues (bei Consumer Queues ist die zugehörige DLQ
  ebenfalls node-lokal/transient und folgt demselben Lebenszyklus).
- DLQ selbst ist eine reguläre Queue (FIFO) und kann wie jede andere konsumiert werden.

## 5. Userverwaltung (OpenID Connect)

- Kein eigenes Credential-Management — Authentifizierung läuft vollständig über OIDC
  (Access Token im HTTP/2-Header bei Connect-Aufbau).
- Eigene Autorisierungsschicht darüber: Mapping von OIDC-Claims (z.B. `sub`, `tenant_id`,
  Rollen/Scopes) auf vhost-Zugriff und Queue-Berechtigungen (publish/consume/manage).
- Token-Validierung am Gateway/Edge-Node (JWKS-Caching), damit Shards selbst
  authentifizierungsfrei bleiben und sich auf Datenpfad konzentrieren können.
- Für SaaS-Onboarding: Self-Service-Erstellung von vhosts/Queues nach Login, verknüpft
  mit Tenant-Claim aus dem Identity Provider.

## 6. Horizontale Skalierung

- **Gateway-Ebene**: zustandslos, beliebig horizontal skalierbar (nur Routing + Auth).
- **Shard-Ebene**: neue vhosts werden neuen (oder least-loaded) Shard-Gruppen zugewiesen;
  Rebalancing durch Verschieben ganzer vhosts (nicht einzelner Messages) zwischen Shards.
- **Elastizität**: neue Shard-Knoten können der Metadaten-Map hinzugefügt werden; Leader-
  Wahl und Log-Replikation starten automatisch für neu zugewiesene vhosts.

## 7. Implementierungsprinzip: Streams-only in .NET, Performance durch In-Memory-Betrieb

- **Grundprinzip: Der Fokus liegt konsequent auf Performance — der Server-Service
  arbeitet, wo immer möglich, vollständig in-memory.** Queue-Zustand (FIFO-Reihenfolge,
  Priority-Index aus Abschnitt 4, Consumer-Offsets, Redelivery-Zähler) lebt im Speicher
  und wird direkt daraus bedient; Disk-I/O (FasterLog, Abschnitt 4) dient ausschließlich
  der Durabilität/Replikation im Hintergrund, nicht dem synchronen Lesepfad für
  Zustellung an Consumer.
- Schreibpfad bleibt dennoch korrekt: Der Raft-Commit (Abschnitt 3, Mehrheits-Ack) ist
  weiterhin Voraussetzung für "zugestellt" bei Sync Queues — In-Memory-Betrieb
  beschleunigt Zustellung/Lookup, ersetzt aber nicht die Persistenzgarantie.
- Consumer Queues (Abschnitt 4a) sind ohnehin komplett in-memory, da sie nie repliziert
  oder auf Disk geschrieben werden — sie profitieren am stärksten von diesem Prinzip.
- Um den Memory-Footprint dabei trotzdem gering zu halten, wird durchgängig mit
  `Stream`-basierten APIs
  entwickelt statt mit vollständig materialisierten Byte-Arrays/Buffern:
  - `PipeReader`/`PipeWriter` (System.IO.Pipelines) für Netzwerk-I/O statt `byte[]`-Kopien.
  - HTTP/2-Request/Response-Bodies werden als Stream verarbeitet (kein Puffern ganzer
    Messages im Speicher, insbesondere bei großen Payloads).
  - Log-Segmente werden per `FileStream`/`SafeFileHandle` sequenziell gelesen/geschrieben,
    nicht komplett in den Speicher geladen.
  - Serialisierung/Deserialisierung arbeitet auf `Span<byte>`/`ReadOnlySequence<byte>`
    statt Zwischenobjekten, wo es die Nachrichtengröße zulässt.
- Ziel: konstanter (oder near-konstanter) Speicherverbrauch pro Connection/Stream,
  unabhängig von Nachrichtengröße oder Anzahl gleichzeitiger Streams — wichtig, da ein
  einzelner Node potenziell tausende Streams über viele vhosts gleichzeitig bedient.
- Konsequenz für Codereview/Design: Kein `ReadAllBytes`/`ToArray()` auf dem Hot-Path,
  keine vollständige Message-Materialisierung, wenn Weiterreichen als Stream möglich ist.

### Nebenläufigkeit: lock-frei statt blockierend

- **Grundprinzip: möglichst keine blockierenden Locks (`lock`, `Monitor`, blockierendes
  `Wait`).** Stattdessen werden .NET-Patterns eingesetzt, die Thread-Safety ohne
  Blockieren sicherstellen:
  - `System.Threading.Channels` als primäres Producer/Consumer-Muster für Worker
    (bereits in Abschnitt 9 für Consumer- und SMQ-Worker festgelegt).
  - `System.Collections.Concurrent`-Typen (`ConcurrentQueue<T>`, `ConcurrentDictionary<K,V>`)
    für gemeinsam genutzten Zustand, z.B. der Bucket-Priority-Index (Abschnitt 4).
  - `Interlocked`-Operationen für einfache Zähler (z.B. Redelivery-Zähler) statt Locks.
  - Wo unvermeidbar Koordination nötig ist (z.B. Raft-Membership-Change, Abschnitt 2a):
    asynchrone Synchronisation über `SemaphoreSlim`/`Channel`-basierte Single-Writer-
    Actor-Muster statt synchroner Locks, die einen Thread blockieren.
- Gilt gleichermaßen für den **.NET-MQ-Client** (Abschnitt 10): auch dort keine
  blockierenden Locks im Hot-Path von Publish/Consume, Reconnect-Logik über
  nebenläufigkeitssichere, nicht-blockierende Zustandsverwaltung (z.B. `Channel`-
  basierte Reconnect-Queue statt gesperrtem Verbindungszustand).
- Begründung: passt zum In-Memory-/Performance-Grundprinzip dieses Abschnitts — Locks
  wären gerade bei vielen gleichzeitigen vhosts/Streams pro Node ein direkter
  Durchsatz- und Latenz-Flaschenhals.

### Async/await statt sequenzieller Programmierung

- **Grundprinzip: `async`/`await` wird durchgängig gegenüber sequenzieller (synchron
  blockierender) Programmierung bevorzugt.** Jeder I/O-gebundene oder wartende Vorgang
  (Netzwerk, Disk, Channel-Reads, Raft-Replikation, gRPC-Streaming) läuft asynchron,
  damit Threads während des Wartens für andere Streams/vhosts freigegeben werden statt
  blockiert zu sein.
- Gilt für Server und Client gleichermaßen (Abschnitt 10): gRPC-Streaming-APIs in .NET
  sind bereits async-nativ (`IAsyncStreamReader`, `IAsyncEnumerable`), Worker (Abschnitt 9)
  verarbeiten Channel-Items über `await foreach`/`ReadAllAsync` statt blockierendem Polling.
- Konsequenz für Codereview/Design: kein `.Result`/`.Wait()` auf Tasks, kein synchrones
  Blockieren auf asynchrone Operationen, `ConfigureAwait(false)` in Bibliothekscode
  (Engine, Client) wo sinnvoll, um unnötige Kontext-Wechsel zu vermeiden.
- Begründung: konsistent mit dem lock-freien, in-memory-fokussierten Performance-Ansatz
  dieses Abschnitts — blockierende sequenzielle Abläufe würden die Skalierbarkeit über
  viele gleichzeitige Streams/vhosts pro Node (Abschnitt 3, 7) direkt untergraben.

## 8. Technologie-Stack

- **Backend**: .NET 10 oder neuer.
- **Logging**: Serilog, strukturiert (kein reines Text-Logging), damit Log-Einträge über
  Gateway, Shards und Admin-Interface hinweg konsistent korrelierbar sind (z.B. per
  vhost, Queue-Name, Message-ID als strukturierte Properties statt in den Log-Text
  eingebettet).
- **Web-Admin-Oberfläche**: Angular als Framework, TailwindCSS für Styling, bei Bedarf
  ergänzt um PrimeNG für komplexere UI-Komponenten (Tabellen, Dialoge, Formulare) —
  insbesondere für die in Abschnitt 4c beschriebene Einsicht in SMQ-Inhalte und generell
  Queue-/DLQ-Monitoring.
- **Observability**: OpenTelemetry für Traces, Metrics und Baggage (durchgängig über
  Gateway → Shard/vhost → Log-Schicht hinweg, z.B. Trace-Kontext über Publish/Deliver/Ack
  propagiert). Logging über OpenTelemetry ist **zukünftig geplant, aber noch nicht
  umgesetzt** — aktuell übernimmt Serilog das Logging eigenständig; eine spätere
  Zusammenführung (Serilog-Sink → OTel-Logs-Pipeline) ist vorgesehen, aber kein Teil des
  aktuellen Scopes.
- **Projektstruktur**: Die MQ-Engine selbst besteht aus **genau einer** .csproj-Datei —
  kein Aufsplitten in mehrere Bibliotheksprojekte. Tests und Benchmarks liegen in
  zusätzlichen, separaten Projekten innerhalb derselben Solution (z.B. `MQ.Tests`,
  `MQ.Benchmarks`), referenzieren aber nur das eine MQ-Projekt. Der .NET-MQ-Client
  (Abschnitt 10) ist ebenfalls ein eigenes, separates Projekt (`MQ.Client`) — er wird
  als eigenständiges NuGet-Package veröffentlicht und ist kein Teil der Engine-Assembly.
- **Deployment/Publish**: Die MQ-Engine wird für den Betrieb als **Single-File-Publish**
  mit **Trimming** gebaut (`dotnet publish -p:PublishSingleFile=true
  -p:PublishTrimmed=true -p:SelfContained=true`), sodass am Ende eine einzige
  ausführbare Datei ohne separate Abhängigkeits-DLLs entsteht. Wichtig für Trimming:
  reflection-basierte APIs (z.B. Protobuf-Reflection, DI-Container-Registrierungen)
  müssen trim-safe annotiert sein (`DynamicallyAccessedMembers`), sonst drohen zur
  Laufzeit entfernte Typen/Member.

## 9. Server-Worker-Modell (System.Threading.Channels)

- Pro **Consumer**, der an einer Queue angehängt ist (die Queue wiederum kann über einen
  Exchange gespeist werden, Abschnitt 1a, oder direkt), erzeugt der Server einen eigenen
  **`System.Threading.Channels`-Worker**. Dieser Worker verwaltet für diesen Consumer die
  Zustellung aus der zugehörigen Queue sowie die zugehörige DLQ (Abschnitt 4d) —
  Redelivery-Zählung und das Verschieben in die DLQ nach x fehlgeschlagenen Versuchen
  laufen isoliert im Kontext dieses Workers.
- Für Nachrichten, die in eine **SMQ** (Abschnitt 4c) geschrieben werden, gibt es einen
  **eigenen, separaten Channel-Worker** — getrennt vom Consumer-Worker, da
  SMQ-Zustellung (zeitgesteuertes Verschieben in die Zielqueue) einem anderen Lebenszyklus
  folgt als reguläre Consumer-Zustellung und keinen Consumer-Bezug hat.
- Vorteil des Channel-basierten Modells: jeder Worker verarbeitet seinen Eingang
  sequenziell und lock-frei (`Channel<T>` als Producer/Consumer-Puffer), Backpressure
  über Channel-Capacity statt eigener Synchronisationsprimitive — passt zum
  Streams-only-Prinzip (Abschnitt 7) und vermeidet globale Locks pro Queue.
- Lebenszyklus: Consumer-Worker wird beim Anhängen des Consumers an die Queue erzeugt
  und bei Disconnect beendet (bei Consumer Queues fällt das mit dem Verwerfen der Queue
  selbst zusammen, Abschnitt 4a); der SMQ-Worker läuft, solange die zugehörige SMQ
  existiert (transient bei Consumer Queues, persistent/repliziert bei Sync Queues).

## 10. .NET-MQ-Client

- Eigenständiges Projekt `MQ.Client` (separates .csproj, siehe Abschnitt 8), als NuGet-
  Package veröffentlicht — keine Abhängigkeit auf die Engine-Assembly selbst, nur auf die
  generierten gRPC-/Protobuf-Contracts (Abschnitt 1).
- Kapselt die gRPC-Streaming-APIs in eine handlichere .NET-Oberfläche:
  - Publish (Broadcast und Queue, inkl. optionaler Delay/Schedule-Parameter für SMQ,
    Abschnitt 4c) als TimeSpan- oder DateTimeOffset-Overload.
  - Subscribe/Consume mit Ack/Nack-Handling, versteckt die Redelivery-/Flow-Control-Details
    (Abschnitt 1, Konsistenzgarantien) hinter einer einfachen Callback- oder
    `IAsyncEnumerable`-basierten API.
  - Auflösen des `ms-link`-Headers (Abschnitt 1) für Nachrichten über dem 1 MB-Limit —
    der Client lädt die referenzierte Payload transparent nach, statt dies dem Aufrufer
    zu überlassen.
- Reconnect-/Redirect-Logik eingebaut: bei Leader-Failover (Abschnitt 3) oder Shard-
  Rebalancing (Abschnitt 2a) verbindet sich der Client automatisch zur aktuellen
  Leader-Adresse aus der Shard-Map neu, ohne dass der Aufrufer das selbst behandeln muss.
- OIDC-Token-Handling (Abschnitt 5): Der Client nimmt ein Access Token (oder einen
  Token-Provider-Delegate für automatischen Refresh) entgegen und hängt es an jede
  Connection an; keine eigene Credential-Verwaltung im Client.
- Folgt demselben Streams-only-Prinzip (Abschnitt 7): keine vollständige Message-
  Materialisierung auf dem Hot-Path, Nutzung der gRPC-Streaming-Reader/Writer direkt.
- Observability (Abschnitt 8): Der Client propagiert OpenTelemetry-Trace-Kontext und
  Baggage automatisch über Publish/Consume hinweg, damit Producer- und Consumer-Traces
  clientseitig bereits korrekt verknüpft sind.

## 11. Metriken-Anforderungen

- **Grundprinzip**: umfassende Metriken über das gesamte System, erhoben via
  **OpenTelemetry Metrics** (Abschnitt 8) — Gateway/Edge-Node und Shards exportieren
  ihre Metriken über dieselbe OTel-Pipeline wie Traces/Baggage, konsistent über vhosts
  und Nodes hinweg abfragbar/aggregierbar.
- **Connections**: aktuelle Anzahl offener Verbindungen (gRPC/HTTP2-Streams) muss als
  Gauge/UpDownCounter pro Node und pro vhost feststellbar sein.
- **Queues**: Anzahl vorhandener Queues (Consumer Queues, Sync Queues, SMQ, DLQ jeweils
  unterscheidbar) pro vhost.
- **Messages pro Queue**: Anzahl Nachrichten aktuell in einer Queue (wartend, noch nicht
  zugestellt).
- **Zustellung**: Anzahl Nachrichten, die aktuell in Zustellung sind (an einen Consumer
  ausgeliefert, aber noch nicht bestätigt/geackt), sowie Anzahl bereits erfolgreich
  zugestellter (geackter) Nachrichten — jeweils als fortlaufende Zähler.
- **Durchsatz-Raten**: Messages/Sekunde getrennt für
  - eingehend (Publish-Rate, Producer → Exchange),
  - ausgehend (Deliver-Rate, Queue → Consumer),
  - wartend (aktueller Füllstand/Wartende pro Sekunde, d.h. Trend der Queue-Tiefe).
- **Erhebung**: Queue-bezogene Zähler (Anzahl Queues, Nachrichten pro Queue, in
  Zustellung, zugestellt) werden durch einfaches Zählen im In-Memory-Zustand der Queue
  ermittelt (konsistent mit dem In-Memory-first-Prinzip aus Abschnitt 7) — kein
  zusätzlicher Lese-Zugriff auf den FIFO-Log für Metrikzwecke, um den Hot-Path nicht zu
  belasten.

## Offene Fragen / nächste Schritte

- Wahl der Raft-Bibliothek für .NET (z.B. `dotnext`, eigene Implementierung, oder
  bestehende Lösung wie etcd als externe Komponente für die Metadaten-Ebene nutzen).
- Exaktes Framing-Protokoll über HTTP/2 spezifizieren (Message-Format, Ack-Semantik,
  Flow-Control-Details).
- Konsistenzgarantien für Broadcast (best-effort) vs. Queue (at-least-once) klar
  dokumentieren.
- Strategie für Sub-Sharding großer Tenants und Rebalancing-Prozess im Detail ausarbeiten.
