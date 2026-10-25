// Invariants of the Order aggregate:
//   1. at most 3 different products (lines)
//   2. total <= 5,000 USD
//   3. lines change only while the order is a draft
//   4. lines are reached only through the root (Order)

var order = Order.Create("ORD-1");
Try("AddLine(KB, 100, qty 2)", () => order.AddLine("KB", 100, 2));
Try("AddLine(MOUSE, 25, qty 4)", () => order.AddLine("MOUSE", 25, 4));
Try("AddLine(KB, 100, qty 1) same SKU", () => order.AddLine("KB", 100, 1));
Try("AddLine(MONITOR, 1500, qty 1)", () => order.AddLine("MONITOR", 1500, 1));
Try("AddLine(CABLE, 5, qty 1) 4th line", () => order.AddLine("CABLE", 5, 1));
Try("ChangeQuantity(MONITOR, 3)", () => order.ChangeQuantity("MONITOR", 3));
Try("ChangeQuantity(MONITOR, 2)", () => order.ChangeQuantity("MONITOR", 2));
order.Place();
Show("Place()", order.Status);
Try("AddLine(CABLE, 5, qty 1) placed", () => order.AddLine("CABLE", 5, 1));

Console.WriteLine();
foreach (var line in order.Lines)
    Show($"line {line.Sku}", $"{line.Quantity} x {line.UnitPrice} = {line.Subtotal}");
Show("Total", order.Total);

void Try(string label, Action action)
{
    try { action(); Show(label, $"ok -> {order.Lines.Count} lines, total {order.Total}"); }
    catch (DomainException ex) { Show(label, $"rejected -> {ex.Message}"); }
}

static void Show(string label, object? value) => Console.WriteLine($"  {label,-36}: {value}");

sealed class DomainException(string message) : Exception(message);

enum OrderStatus { Draft, Placed }

// Aggregate root: the only entry point
sealed class Order
{
    public const int MaxLines = 3;
    public const decimal MaxTotal = 5000;

    private readonly List<OrderLine> _lines = [];

    private Order(string id) => Id = id;

    public string Id { get; }
    public OrderStatus Status { get; private set; } = OrderStatus.Draft;
    public IReadOnlyList<OrderLine> Lines => _lines.AsReadOnly();
    public decimal Total => _lines.Sum(l => l.Subtotal);

    public static Order Create(string id) => new(id);

    public void AddLine(string sku, decimal unitPrice, int quantity)
    {
        EnsureDraft();
        EnsureTotalWithinLimit(Total + unitPrice * quantity);

        var existing = _lines.FirstOrDefault(l => l.Sku == sku);
        if (existing is not null)
        {
            existing.SetQuantity(existing.Quantity + quantity);
            return;
        }

        if (_lines.Count == MaxLines)
            throw new DomainException($"max {MaxLines} lines per order");
        _lines.Add(new OrderLine(sku, unitPrice, quantity));
    }

    public void ChangeQuantity(string sku, int quantity)
    {
        EnsureDraft();
        var line = _lines.Single(l => l.Sku == sku);
        EnsureTotalWithinLimit(Total - line.Subtotal + line.UnitPrice * quantity);
        line.SetQuantity(quantity);
    }

    public void Place()
    {
        EnsureDraft();
        if (_lines.Count == 0)
            throw new DomainException("an empty order cannot be placed");
        Status = OrderStatus.Placed;
    }

    private void EnsureDraft()
    {
        if (Status != OrderStatus.Draft)
            throw new DomainException("a placed order cannot change");
    }

    private static void EnsureTotalWithinLimit(decimal newTotal)
    {
        if (newTotal > MaxTotal)
            throw new DomainException($"total {newTotal} would exceed {MaxTotal}");
    }
}

// Inner entity: created and changed only by the root (internal members)
sealed class OrderLine
{
    internal OrderLine(string sku, decimal unitPrice, int quantity)
    {
        Sku = sku;
        UnitPrice = unitPrice;
        SetQuantity(quantity);
    }

    public string Sku { get; }
    public decimal UnitPrice { get; }
    public int Quantity { get; private set; }
    public decimal Subtotal => UnitPrice * Quantity;

    internal void SetQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("quantity must be positive");
        Quantity = quantity;
    }
}
