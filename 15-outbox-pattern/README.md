# 15 · Outbox Pattern

**Definition:** save the aggregate **and** the outgoing message in the **same database transaction** (message in an `Outbox` table). A separate relay reads the outbox and publishes to the broker, retrying until it succeeds. This gives **at-least-once** delivery, so consumers must be idempotent.

## 1. Example

```
Initial data:
  broker               : down for the first publish, then up

Commands (❌ dual write):
  save ORD-1           : committed
  broker.Publish       : FAILED
  => orders=1, events delivered=0          (event LOST forever)

Commands (✅ outbox):
  tx: ORD-2 + outbox row: committed together -> orders=1, pending=1
  relay run #1         : broker down -> delivered=0, pending=1, attempts=1
  relay run #2         : broker up   -> delivered=1, pending=0, attempts=2

Commands (✅ rollback):
  tx: ORD-3 + outbox, then "credit limit exceeded" -> orders=0, outbox=0

Commands (✅ duplicate):
  same message id delivered twice -> consumer processed 1

Result:
  no lost events, no ghost events, duplicates ignored
```

## 2. Diagram

```mermaid
sequenceDiagram
  participant App
  participant DB as Database
  participant R as Outbox relay
  participant B as Broker
  participant C as Consumer
  App->>DB: BEGIN; INSERT order; INSERT outbox; COMMIT
  loop every N seconds
    R->>DB: SELECT unsent outbox rows
    R->>B: publish
    alt broker down
      B-->>R: error -> stays pending, retry next run
    else ok
      B-->>R: ack
      R->>DB: mark row sent
    end
  end
  B->>C: message (maybe twice)
  C->>C: skip if MessageId already processed
```

## 3. Code

```csharp
// One transaction: both rows or none
db.Transaction(tx =>
{
    tx.Orders.Add("ORD-2");
    tx.Outbox.Add(new OutboxMessage(messageId, "OrderPlaced", "ORD-2"));
});

// Relay: publish pending rows, mark only on success
foreach (var message in db.Outbox.Where(m => !m.Processed))
{
    message.Attempts++;
    try { broker.Publish(message); message.Processed = true; }
    catch (BrokerDownException) { /* stay pending; next run retries */ }
}

// Consumer: idempotent by message id
if (!_seen.Add(message.Id)) return;
```

Run: `dotnet run --project 15-outbox-pattern`

```
❌ Dual write: save to DB, then publish to broker (two separate systems)
  broker.Publish          : FAILED (broker down)
  orders in DB            : 1
  events delivered        : 0
  => order exists, nobody heard about it. Event LOST.

✅ Outbox: order + message saved in ONE transaction, relay publishes later
  after commit            : orders=1, outbox pending=1
  relay run #1            : delivered=0, pending=1, attempts=1
  relay run #2            : delivered=1, pending=0, attempts=2

✅ Rollback: if the order fails, the message is never stored either
  transaction             : rolled back (credit limit exceeded)
  orders / outbox         : 0 / 0

✅ At-least-once: relay crashed after publish, before marking => duplicate; consumer dedupes
  deliveries / processed  : 2 / 1
```

## ✅ vs ❌

| ✅ Correct | ❌ Wrong | Why |
|---|---|---|
| order + outbox row in **one** transaction | save, then publish (dual write) | crash between them = lost event |
| publish **after** commit (relay) | publish inside the transaction | rollback = ghost event already sent |
| consumer dedupes by `MessageId` (inbox table) | assume exactly-once | relay can crash after publish, before marking |
| tools: MassTransit / NServiceBus / Wolverine outbox, Debezium CDC | hand-rolled relay without retries/monitoring | stuck messages nobody sees |

**Next →** [16 CQRS](../16-cqrs/)
