Console.WriteLine("❌ Anemic model: data bag + service, rules are optional");
var anemic = new AnemicOrder();
anemic.Total = -50;
Show("anemic.Total = -50", $"accepted, Total = {anemic.Total}");
AnemicOrderService.Pay(anemic);
Show("service.Pay(order with 0 lines)", $"accepted, Status = {anemic.Status}");
anemic.Status = "Shipped-ish";
Show("anemic.Status = \"Shipped-ish\"", $"accepted, Status = {anemic.Status}");

Console.WriteLine();
Console.WriteLine("✅ Rich model: the object protects its own rules");
var rich = RichOrder.Create();
Try("rich.Pay() with 0 lines", () => rich.Pay());
Try("rich.AddLine(-50)", () => rich.AddLine(-50));
rich.AddLine(30);
rich.AddLine(20);
Show("rich.AddLine(30), AddLine(20)", $"Total = {rich.Total} (calculated, never set)");
rich.Pay();
Show("rich.Pay()", $"Status = {rich.Status}");
Try("rich.AddLine(10) after pay", () => rich.AddLine(10));

static void Show(string label, object? value) => Console.WriteLine($"  {label,-32}: {value}");

static void Try(string label, Action action)
{
    try { action(); Show(label, "accepted"); }
    catch (InvalidOperationException ex) { Show(label, $"rejected -> {ex.Message}"); }
}

// ❌ Anemic: public setters, no behavior
sealed class AnemicOrder
{
    public List<decimal> LinePrices { get; set; } = [];
    public decimal Total { get; set; }
    public string Status { get; set; } = "Draft";
}

static class AnemicOrderService
{
    public static void Pay(AnemicOrder order) => order.Status = "Paid"; // forgot to check lines
}

// ✅ Rich: private setters, behavior + rules in one place
enum OrderStatus { Draft, Paid }

sealed class RichOrder
{
    private readonly List<decimal> _linePrices = [];

    private RichOrder() { }

    public OrderStatus Status { get; private set; } = OrderStatus.Draft;
    public decimal Total => _linePrices.Sum();

    public static RichOrder Create() => new();

    public void AddLine(decimal price)
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException("a paid order cannot change");
        if (price <= 0)
            throw new InvalidOperationException("price must be positive");
        _linePrices.Add(price);
    }

    public void Pay()
    {
        if (_linePrices.Count == 0)
            throw new InvalidOperationException("an empty order cannot be paid");
        Status = OrderStatus.Paid;
    }
}
