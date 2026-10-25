var catalog = new ProductCatalog(new()
{
    ["KB"] = 100,
    ["MOUSE"] = 25,
});
var factory = new OrderFactory(catalog);

Console.WriteLine("✅ Simple creation: static Create() on the aggregate");
var draft = Order.Create("ORD-1", "sara");
Show("Order.Create(ORD-1, sara)", $"{draft.Lines.Count} lines");

Console.WriteLine();
Console.WriteLine("✅ Complex creation: factory builds a valid order from a cart");
var cart = new Cart("omar", [new("KB", 2), new("MOUSE", 1)]);
var order = factory.FromCart("ORD-2", cart);
foreach (var line in order.Lines)
    Show($"line {line.Sku}", $"{line.Quantity} x {line.UnitPrice}");
Show("total", order.Total);

catalog.ChangePrice("KB", 120);
Show("catalog: KB price -> 120", $"order total still {order.Total} (price snapshot)");

Try("FromCart(empty cart)", () => factory.FromCart("ORD-3", new Cart("omar", [])));
Try("FromCart(unknown SKU 'TV')", () => factory.FromCart("ORD-4", new Cart("omar", [new("TV", 1)])));

static void Show(string label, object? value) => Console.WriteLine($"  {label,-28}: {value}");

static void Try(string label, Action action)
{
    try { action(); Show(label, "accepted"); }
    catch (InvalidOperationException ex) { Show(label, $"rejected -> {ex.Message}"); }
}

sealed record CartItem(string Sku, int Quantity);
sealed record Cart(string CustomerId, IReadOnlyList<CartItem> Items);

sealed class ProductCatalog(Dictionary<string, decimal> prices)
{
    public decimal? PriceOf(string sku) => prices.TryGetValue(sku, out var p) ? p : null;
    public void ChangePrice(string sku, decimal price) => prices[sku] = price;
}

// Factory: knows HOW to assemble an Order from other objects
sealed class OrderFactory(ProductCatalog catalog)
{
    public Order FromCart(string orderId, Cart cart)
    {
        if (cart.Items.Count == 0)
            throw new InvalidOperationException("cart is empty");

        var order = Order.Create(orderId, cart.CustomerId);
        foreach (var item in cart.Items)
        {
            var price = catalog.PriceOf(item.Sku)
                ?? throw new InvalidOperationException($"unknown product '{item.Sku}'");
            order.AddLine(item.Sku, price, item.Quantity);
        }
        return order;
    }
}

sealed class Order
{
    private readonly List<OrderLine> _lines = [];

    private Order(string id, string customerId)
    {
        Id = id;
        CustomerId = customerId;
    }

    public string Id { get; }
    public string CustomerId { get; }
    public IReadOnlyList<OrderLine> Lines => _lines.AsReadOnly();
    public decimal Total => _lines.Sum(l => l.UnitPrice * l.Quantity);

    public static Order Create(string id, string customerId) => new(id, customerId);

    public void AddLine(string sku, decimal unitPrice, int quantity) =>
        _lines.Add(new OrderLine(sku, unitPrice, quantity));
}

sealed record OrderLine(string Sku, decimal UnitPrice, int Quantity);
