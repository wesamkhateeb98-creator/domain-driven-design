var writeStore = new Dictionary<string, Order>();     // write DB: normalized aggregates
var readStore = new ReadStore();                     // read DB: denormalized, query-shaped
var projector = new OrderProjector(readStore);
var commands = new OrderCommandHandler(writeStore, projector);
var queries = new OrderQueries(readStore);

// --- Commands: change state, return nothing (or an id) ---
commands.Handle(new PlaceOrder("ORD-1", "Sara", [new("KB", 100, 2), new("MOUSE", 25, 1)]));
commands.Handle(new PlaceOrder("ORD-2", "Omar", [new("MONITOR", 1500, 1)]));
commands.Handle(new PlaceOrder("ORD-3", "Sara", [new("CABLE", 5, 4)]));
commands.Handle(new CancelOrder("ORD-2"));

// --- Queries: read state, change nothing ---
Console.WriteLine("Query: GetOrderSummary(ORD-1)");
var s = queries.GetOrderSummary("ORD-1")!;
Console.WriteLine($"  {s.OrderId} | {s.CustomerName} | {s.ItemCount} items | {s.Total} USD | {s.Status}");

Console.WriteLine();
Console.WriteLine("Query: GetCustomerTotals() (only non-cancelled orders)");
foreach (var c in queries.GetCustomerTotals())
    Console.WriteLine($"  {c.CustomerName,-5} | {c.OrderCount} orders | {c.TotalSpent} USD");

Console.WriteLine();
Console.WriteLine($"Write model rows: {writeStore.Count} Order aggregates with {writeStore.Values.Sum(o => o.Lines.Count)} lines");
Console.WriteLine($"Read model rows : {readStore.Summaries.Count} summaries + {readStore.CustomerTotals.Count} customer totals");

// ===== Write side =====
sealed record OrderLine(string Sku, decimal UnitPrice, int Quantity);
sealed record PlaceOrder(string OrderId, string CustomerName, List<OrderLine> Lines);
sealed record CancelOrder(string OrderId);

sealed record OrderPlaced(string OrderId, string CustomerName, int ItemCount, decimal Total);
sealed record OrderCancelled(string OrderId);

sealed class Order
{
    private Order(string id, string customerName, List<OrderLine> lines)
    {
        Id = id;
        CustomerName = customerName;
        Lines = lines;
    }

    public string Id { get; }
    public string CustomerName { get; }
    public List<OrderLine> Lines { get; }
    public bool IsCancelled { get; private set; }

    public static Order Place(string id, string customerName, List<OrderLine> lines)
    {
        if (lines.Count == 0)
            throw new InvalidOperationException("empty order");
        return new Order(id, customerName, lines);
    }

    public void Cancel() => IsCancelled = true;
}

sealed class OrderCommandHandler(Dictionary<string, Order> writeStore, OrderProjector projector)
{
    public void Handle(PlaceOrder cmd)
    {
        var order = Order.Place(cmd.OrderId, cmd.CustomerName, cmd.Lines);
        writeStore[order.Id] = order;
        projector.On(new OrderPlaced(order.Id, order.CustomerName,
            order.Lines.Sum(l => l.Quantity), order.Lines.Sum(l => l.UnitPrice * l.Quantity)));
    }

    public void Handle(CancelOrder cmd)
    {
        writeStore[cmd.OrderId].Cancel();
        projector.On(new OrderCancelled(cmd.OrderId));
    }
}

// ===== Read side =====
sealed record OrderSummary(string OrderId, string CustomerName, int ItemCount, decimal Total, string Status);
sealed record CustomerTotal(string CustomerName, int OrderCount, decimal TotalSpent);

sealed class ReadStore
{
    public Dictionary<string, OrderSummary> Summaries { get; } = [];
    public Dictionary<string, CustomerTotal> CustomerTotals { get; } = [];
}

// Projector: turns events into ready-to-read rows
sealed class OrderProjector(ReadStore store)
{
    public void On(OrderPlaced e)
    {
        store.Summaries[e.OrderId] = new OrderSummary(e.OrderId, e.CustomerName, e.ItemCount, e.Total, "Placed");
        var current = store.CustomerTotals.GetValueOrDefault(e.CustomerName) ?? new CustomerTotal(e.CustomerName, 0, 0);
        store.CustomerTotals[e.CustomerName] = current with
        {
            OrderCount = current.OrderCount + 1,
            TotalSpent = current.TotalSpent + e.Total,
        };
    }

    public void On(OrderCancelled e)
    {
        var summary = store.Summaries[e.OrderId];
        store.Summaries[e.OrderId] = summary with { Status = "Cancelled" };
        var current = store.CustomerTotals[summary.CustomerName];
        store.CustomerTotals[summary.CustomerName] = current with
        {
            OrderCount = current.OrderCount - 1,
            TotalSpent = current.TotalSpent - summary.Total,
        };
    }
}

sealed class OrderQueries(ReadStore store)
{
    public OrderSummary? GetOrderSummary(string orderId) => store.Summaries.GetValueOrDefault(orderId);

    public IEnumerable<CustomerTotal> GetCustomerTotals() =>
        store.CustomerTotals.Values.OrderByDescending(c => c.TotalSpent);
}
