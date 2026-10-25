var store = new EventStore();
const string stream = "order-ORD-9";

// 1. Create and change the order: every change becomes an event
var order = Order.Create("ORD-9", "sara");
order.AddLine("KB", 100, 2);
order.AddLine("MOUSE", 25, 1);
order.RemoveLine("MOUSE");
order.AddLine("MONITOR", 1500, 1);
order.Place();
store.Append(stream, expectedVersion: order.LoadedVersion, order.UncommittedEvents);
order.MarkCommitted();

Console.WriteLine($"Event stream '{stream}'");
foreach (var e in store.Load(stream))
    Console.WriteLine($"  v{e.Version}: {e.Event}");

// 2. Rebuild current state by replaying all events
var current = Order.Rehydrate(store.Load(stream).Select(e => e.Event));
Console.WriteLine();
Console.WriteLine("Replay all events  -> current state");
Console.WriteLine($"  status={current.Status}, lines={current.LineCount}, total={current.Total}, version={current.Version}");

// 3. Time travel: replay only the first 3 events
var past = Order.Rehydrate(store.Load(stream).Take(3).Select(e => e.Event));
Console.WriteLine("Replay first 3     -> state at v3");
Console.WriteLine($"  status={past.Status}, lines={past.LineCount}, total={past.Total}, version={past.Version}");

// 4. Optimistic concurrency: two users loaded v6, both try to write
Console.WriteLine();
var userA = Order.Rehydrate(store.Load(stream).Select(e => e.Event));
var userB = Order.Rehydrate(store.Load(stream).Select(e => e.Event));
userA.Cancel("customer request");
store.Append(stream, expectedVersion: userA.LoadedVersion, userA.UncommittedEvents);
Console.WriteLine($"  user A appends at expected v{userA.LoadedVersion}: ok, stream now v{store.Load(stream).Count}");
userB.Cancel("duplicate order");
try
{
    store.Append(stream, expectedVersion: userB.LoadedVersion, userB.UncommittedEvents);
}
catch (ConcurrencyException ex)
{
    Console.WriteLine($"  user B appends at expected v{userB.LoadedVersion}: rejected -> {ex.Message}");
}

// ===== Events: facts, past tense, never changed or deleted =====
interface IEvent;
sealed record OrderCreated(string OrderId, string CustomerId) : IEvent;
sealed record LineAdded(string Sku, decimal UnitPrice, int Quantity) : IEvent;
sealed record LineRemoved(string Sku) : IEvent;
sealed record OrderPlaced : IEvent;
sealed record OrderCancelled(string Reason) : IEvent;

enum OrderStatus { Draft, Placed, Cancelled }

sealed class Order
{
    private readonly Dictionary<string, (decimal Price, int Qty)> _lines = [];
    private readonly List<IEvent> _uncommitted = [];

    private Order() { }

    public string Id { get; private set; } = "";
    public OrderStatus Status { get; private set; }
    public int Version { get; private set; }
    public int LoadedVersion => Version - _uncommitted.Count; // version read from the store
    public int LineCount => _lines.Count;
    public decimal Total => _lines.Values.Sum(l => l.Price * l.Qty);
    public IReadOnlyList<IEvent> UncommittedEvents => _uncommitted.AsReadOnly();

    // --- Decide: check rules, then record an event (no direct state change) ---
    public static Order Create(string id, string customerId)
    {
        var order = new Order();
        order.Record(new OrderCreated(id, customerId));
        return order;
    }

    public void AddLine(string sku, decimal price, int qty) { EnsureDraft(); Record(new LineAdded(sku, price, qty)); }
    public void RemoveLine(string sku) { EnsureDraft(); Record(new LineRemoved(sku)); }
    public void Place() { EnsureDraft(); Record(new OrderPlaced()); }

    public void Cancel(string reason)
    {
        if (Status != OrderStatus.Placed)
            throw new InvalidOperationException("only placed orders can be cancelled");
        Record(new OrderCancelled(reason));
    }

    // --- Evolve: the ONLY place state changes ---
    private void Apply(IEvent e)
    {
        switch (e)
        {
            case OrderCreated c: Id = c.OrderId; Status = OrderStatus.Draft; break;
            case LineAdded a: _lines[a.Sku] = (a.UnitPrice, a.Quantity); break;
            case LineRemoved r: _lines.Remove(r.Sku); break;
            case OrderPlaced: Status = OrderStatus.Placed; break;
            case OrderCancelled: Status = OrderStatus.Cancelled; break;
        }
        Version++;
    }

    public static Order Rehydrate(IEnumerable<IEvent> history)
    {
        var order = new Order();
        foreach (var e in history)
            order.Apply(e);
        return order;
    }

    public void MarkCommitted() => _uncommitted.Clear();

    private void Record(IEvent e)
    {
        _uncommitted.Add(e);
        Apply(e);
    }

    private void EnsureDraft()
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException("order is not a draft");
    }
}

// ===== Event store: append-only list per stream =====
sealed record StoredEvent(int Version, IEvent Event);

sealed class ConcurrencyException(string message) : Exception(message);

sealed class EventStore
{
    private readonly Dictionary<string, List<StoredEvent>> _streams = [];

    // expectedVersion = the version the writer loaded; if someone appended since, reject
    public void Append(string streamId, int expectedVersion, IReadOnlyList<IEvent> newEvents)
    {
        if (!_streams.TryGetValue(streamId, out var events))
            _streams[streamId] = events = [];

        if (events.Count != expectedVersion)
            throw new ConcurrencyException($"stream is at v{events.Count}, you loaded v{expectedVersion}");

        foreach (var e in newEvents)
            events.Add(new StoredEvent(events.Count + 1, e));
    }

    public IReadOnlyList<StoredEvent> Load(string streamId) =>
        _streams.TryGetValue(streamId, out var events) ? events.AsReadOnly() : [];
}
