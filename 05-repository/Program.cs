using System.Text.Json;

IOrderRepository orders = new InMemoryOrderRepository();
var ct = CancellationToken.None;

var order = Order.Create(new OrderId("ORD-1001"));
order.AddLine("KB", 100, 2);
order.AddLine("MOUSE", 25, 1);
await orders.SaveAsync(order, ct);
Show("SaveAsync(ORD-1001)", $"{order.Lines.Count} lines, total {order.Total}");

var copy = await orders.GetAsync(new OrderId("ORD-1001"), ct);
copy!.AddLine("CABLE", 5, 3);
Show("GetAsync + AddLine(CABLE) no save", $"in memory: {copy.Lines.Count} lines");

var reloaded = await orders.GetAsync(new OrderId("ORD-1001"), ct);
Show("GetAsync again", $"stored: {reloaded!.Lines.Count} lines (change not saved)");

await orders.SaveAsync(copy, ct);
var afterSave = await orders.GetAsync(new OrderId("ORD-1001"), ct);
Show("SaveAsync(copy) + GetAsync", $"stored: {afterSave!.Lines.Count} lines, total {afterSave.Total}");

var missing = await orders.GetAsync(new OrderId("ORD-404"), ct);
Show("GetAsync(ORD-404)", missing is null ? "null" : "found");

Console.WriteLine();
Console.WriteLine("  Stored row (whole aggregate in one document):");
Console.WriteLine("  " + ((InMemoryOrderRepository)orders).RawRow(new OrderId("ORD-1001")));

static void Show(string label, object? value) => Console.WriteLine($"  {label,-34}: {value}");

readonly record struct OrderId(string Value);

// The domain owns the interface: it speaks in aggregates, not tables
interface IOrderRepository
{
    Task<Order?> GetAsync(OrderId id, CancellationToken ct);
    Task SaveAsync(Order order, CancellationToken ct);
}

// Infrastructure implements it (swap for EF Core / Mongo later)
sealed class InMemoryOrderRepository : IOrderRepository
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };
    private readonly Dictionary<OrderId, string> _rows = [];

    public Task<Order?> GetAsync(OrderId id, CancellationToken ct)
    {
        if (!_rows.TryGetValue(id, out var json))
            return Task.FromResult<Order?>(null);
        var snapshot = JsonSerializer.Deserialize<OrderSnapshot>(json, Json)!;
        return Task.FromResult<Order?>(Order.FromSnapshot(snapshot));
    }

    public Task SaveAsync(Order order, CancellationToken ct)
    {
        _rows[order.Id] = JsonSerializer.Serialize(order.ToSnapshot(), Json);
        return Task.CompletedTask;
    }

    public string RawRow(OrderId id) => _rows[id];
}

sealed record OrderSnapshot(string Id, List<OrderLineSnapshot> Lines);
sealed record OrderLineSnapshot(string Sku, decimal UnitPrice, int Quantity);

sealed class Order
{
    private readonly List<OrderLine> _lines = [];

    private Order(OrderId id) => Id = id;

    public OrderId Id { get; }
    public IReadOnlyList<OrderLine> Lines => _lines.AsReadOnly();
    public decimal Total => _lines.Sum(l => l.UnitPrice * l.Quantity);

    public static Order Create(OrderId id) => new(id);

    public void AddLine(string sku, decimal unitPrice, int quantity) =>
        _lines.Add(new OrderLine(sku, unitPrice, quantity));

    internal OrderSnapshot ToSnapshot() =>
        new(Id.Value, _lines.Select(l => new OrderLineSnapshot(l.Sku, l.UnitPrice, l.Quantity)).ToList());

    internal static Order FromSnapshot(OrderSnapshot snapshot)
    {
        var order = new Order(new OrderId(snapshot.Id));
        foreach (var line in snapshot.Lines)
            order._lines.Add(new OrderLine(line.Sku, line.UnitPrice, line.Quantity));
        return order;
    }
}

sealed record OrderLine(string Sku, decimal UnitPrice, int Quantity);
