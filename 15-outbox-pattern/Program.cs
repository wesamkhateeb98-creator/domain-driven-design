Console.WriteLine("❌ Dual write: save to DB, then publish to broker (two separate systems)");
{
    var db = new FakeDatabase();
    var broker = new FlakyBroker(failuresBeforeSuccess: 1);
    db.Transaction(tx => tx.Orders.Add("ORD-1"));
    try { broker.Publish(new OutboxMessage(Guid.Empty, "OrderPlaced", "ORD-1")); }
    catch (BrokerDownException) { Console.WriteLine("  broker.Publish          : FAILED (broker down)"); }
    Show("orders in DB", db.Orders.Count);
    Show("events delivered", broker.Delivered.Count);
    Console.WriteLine("  => order exists, nobody heard about it. Event LOST.");
}

Console.WriteLine();
Console.WriteLine("✅ Outbox: order + message saved in ONE transaction, relay publishes later");
{
    var db = new FakeDatabase();
    var broker = new FlakyBroker(failuresBeforeSuccess: 1);
    var relay = new OutboxRelay(db, broker);
    var messageId = new Guid("11111111-1111-1111-1111-111111111111");

    db.Transaction(tx =>
    {
        tx.Orders.Add("ORD-2");
        tx.Outbox.Add(new OutboxMessage(messageId, "OrderPlaced", "ORD-2"));
    });
    Show("after commit", $"orders={db.Orders.Count}, outbox pending={db.PendingCount}");

    relay.Run();
    Show("relay run #1", $"delivered={broker.Delivered.Count}, pending={db.PendingCount}, attempts={db.Outbox[0].Attempts}");
    relay.Run();
    Show("relay run #2", $"delivered={broker.Delivered.Count}, pending={db.PendingCount}, attempts={db.Outbox[0].Attempts}");
}

Console.WriteLine();
Console.WriteLine("✅ Rollback: if the order fails, the message is never stored either");
{
    var db = new FakeDatabase();
    try
    {
        db.Transaction(tx =>
        {
            tx.Orders.Add("ORD-3");
            tx.Outbox.Add(new OutboxMessage(Guid.NewGuid(), "OrderPlaced", "ORD-3"));
            throw new InvalidOperationException("credit limit exceeded");
        });
    }
    catch (InvalidOperationException ex) { Show("transaction", $"rolled back ({ex.Message})"); }
    Show("orders / outbox", $"{db.Orders.Count} / {db.Outbox.Count}");
}

Console.WriteLine();
Console.WriteLine("✅ At-least-once: relay crashed after publish, before marking => duplicate; consumer dedupes");
{
    var consumer = new IdempotentConsumer();
    var message = new OutboxMessage(new Guid("22222222-2222-2222-2222-222222222222"), "OrderPlaced", "ORD-4");
    consumer.Handle(message);
    consumer.Handle(message); // redelivered
    Show("deliveries / processed", $"2 / {consumer.ProcessedCount}");
}

static void Show(string label, object? value) => Console.WriteLine($"  {label,-24}: {value}");

sealed record OutboxMessage(Guid Id, string Type, string Payload)
{
    public int Attempts { get; set; }
    public bool Processed { get; set; }
}

sealed class FakeDatabase
{
    public List<string> Orders { get; private set; } = [];
    public List<OutboxMessage> Outbox { get; private set; } = [];
    public int PendingCount => Outbox.Count(m => !m.Processed);

    // All-or-nothing: work on copies, swap them in only if no exception
    public void Transaction(Action<FakeDatabase> work)
    {
        var tx = new FakeDatabase { Orders = [.. Orders], Outbox = [.. Outbox] };
        work(tx);
        Orders = tx.Orders;
        Outbox = tx.Outbox;
    }
}

sealed class BrokerDownException : Exception;

sealed class FlakyBroker(int failuresBeforeSuccess)
{
    private int _failuresLeft = failuresBeforeSuccess;
    public List<OutboxMessage> Delivered { get; } = [];

    public void Publish(OutboxMessage message)
    {
        if (_failuresLeft-- > 0)
            throw new BrokerDownException();
        Delivered.Add(message);
    }
}

sealed class OutboxRelay(FakeDatabase db, FlakyBroker broker)
{
    public void Run()
    {
        foreach (var message in db.Outbox.Where(m => !m.Processed))
        {
            message.Attempts++;
            try
            {
                broker.Publish(message);
                message.Processed = true;
            }
            catch (BrokerDownException)
            {
                // stay pending; the next run retries
            }
        }
    }
}

sealed class IdempotentConsumer
{
    private readonly HashSet<Guid> _seen = [];
    public int ProcessedCount { get; private set; }

    public void Handle(OutboxMessage message)
    {
        if (!_seen.Add(message.Id))
            return;
        ProcessedCount++;
    }
}
