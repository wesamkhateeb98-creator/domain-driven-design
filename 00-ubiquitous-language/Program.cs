// Glossary agreed with the domain expert (see README.md):
//   Order   : a customer's request to buy products
//   Place   : the customer confirms the order; its products can no longer change
//   Ship    : the warehouse hands a placed order to the courier
//   Cancel  : the customer withdraws a placed order before it ships

Console.WriteLine("✅ Ubiquitous language: code reads like the glossary");
var order = Order.Start("ORD-1");
Show("Order.Start(\"ORD-1\")", order.Status);
order.AddProduct("Keyboard");
Show("order.AddProduct(\"Keyboard\")", $"{order.Products.Count} product");
order.Place();
Show("order.Place()", order.Status);
Try("order.AddProduct(\"Mouse\")", () => order.AddProduct("Mouse"));
order.Ship();
Show("order.Ship()", order.Status);
Try("order.Cancel()", () => order.Cancel());

Console.WriteLine();
Console.WriteLine("❌ Developer language: same rules, nobody in the business can read them");
var row = new OrderRow { Id = "ORD-2", S = 0 };
OrderProcessor.Process(row, 1);
Show("OrderProcessor.Process(row, 1)", $"row.S = {row.S}  (what is 1? what is S?)");

static void Show(string label, object? value) => Console.WriteLine($"  {label,-32}: {value}");

static void Try(string label, Action action)
{
    try { action(); Show(label, "accepted"); }
    catch (InvalidOperationException ex) { Show(label, $"rejected -> {ex.Message}"); }
}

enum OrderStatus { Draft, Placed, Shipped, Cancelled }

sealed class Order
{
    private readonly List<string> _products = [];

    private Order(string id) => Id = id;

    public string Id { get; }
    public OrderStatus Status { get; private set; } = OrderStatus.Draft;
    public IReadOnlyList<string> Products => _products.AsReadOnly();

    public static Order Start(string id) => new(id);

    public void AddProduct(string product)
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException("only a draft order can change its products");
        _products.Add(product);
    }

    public void Place()
    {
        if (_products.Count == 0)
            throw new InvalidOperationException("an empty order cannot be placed");
        Status = OrderStatus.Placed;
    }

    public void Ship()
    {
        if (Status != OrderStatus.Placed)
            throw new InvalidOperationException("only a placed order can ship");
        Status = OrderStatus.Shipped;
    }

    public void Cancel()
    {
        if (Status != OrderStatus.Placed)
            throw new InvalidOperationException($"a {Status} order cannot be cancelled (shipped orders are returned)");
        Status = OrderStatus.Cancelled;
    }
}

// ❌ The same model written in "developer language"
sealed class OrderRow
{
    public string Id { get; set; } = "";
    public int S { get; set; } // 0 = draft, 1 = placed, 2 = shipped, 3 = cancelled ... maybe
}

static class OrderProcessor
{
    public static void Process(OrderRow row, int action) => row.S = action;
}
