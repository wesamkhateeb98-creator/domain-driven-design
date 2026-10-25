using System.Text.Json;

var now = new DateTime(2026, 10, 25, 10, 0, 0, DateTimeKind.Utc);
var bus = new InMemoryMessageBus();
var shipping = new ShippingContext.ShipmentHandler(bus);
var billing = new BillingContext.InvoiceHandler(bus);

// --- Sales context ---
var order = new SalesContext.Order("ORD-7", "CUST-3");
order.AddLine("KB", 100, 2);
order.AddLine("MOUSE", 25, 1);
var domainEvent = order.Place(now);
Show("Sales domain event", $"{domainEvent.GetType().Name} with {domainEvent.Lines.Count} rich OrderLine objects");

var integrationEvent = SalesContext.OrderPlacedMapper.ToIntegrationEvent(domainEvent);
bus.Publish("sales.order-placed.v1", integrationEvent);

Console.WriteLine();
Show("Shipping state", string.Join(", ", shipping.Shipments));
Show("Billing state", string.Join(", ", billing.Invoices));

static void Show(string label, object? value) => Console.WriteLine($"  {label,-20}: {value}");

// Integration event = public contract: primitives only, versioned, stable
sealed record OrderPlacedV1(string OrderId, string CustomerId, decimal TotalAmount, string Currency, string[] Skus, DateTime OccurredAtUtc);

sealed class InMemoryMessageBus
{
    private readonly Dictionary<string, List<Action<string>>> _subscribers = [];

    public void Publish<T>(string topic, T message)
    {
        var json = JsonSerializer.Serialize(message); // crosses the boundary as bytes, not objects
        Console.WriteLine($"  [bus] {topic}: {json}");
        foreach (var subscriber in _subscribers.GetValueOrDefault(topic) ?? [])
            subscriber(json);
    }

    public void Subscribe(string topic, Action<string> handler)
    {
        if (!_subscribers.TryGetValue(topic, out var list))
            _subscribers[topic] = list = [];
        list.Add(handler);
    }
}

namespace SalesContext
{
    sealed record OrderLine(string Sku, decimal UnitPrice, int Quantity);

    // Domain event = internal: rich types, may change any time
    sealed record OrderPlaced(string OrderId, string CustomerId, IReadOnlyList<OrderLine> Lines, DateTime OccurredAt)
    {
        public decimal Total => Lines.Sum(l => l.UnitPrice * l.Quantity);
    }

    sealed class Order(string id, string customerId)
    {
        private readonly List<OrderLine> _lines = [];

        public void AddLine(string sku, decimal unitPrice, int quantity) => _lines.Add(new(sku, unitPrice, quantity));

        public OrderPlaced Place(DateTime now) => new(id, customerId, _lines.ToList(), now);
    }

    static class OrderPlacedMapper
    {
        public static OrderPlacedV1 ToIntegrationEvent(OrderPlaced e) =>
            new(e.OrderId, e.CustomerId, e.Total, "USD", e.Lines.Select(l => l.Sku).ToArray(), e.OccurredAt);
    }
}

namespace ShippingContext
{
    // Shipping keeps its OWN copy of the contract fields it needs
    sealed record OrderPlacedMessage(string OrderId, string[] Skus);

    sealed class ShipmentHandler
    {
        public List<string> Shipments { get; } = [];

        public ShipmentHandler(InMemoryMessageBus bus) => bus.Subscribe("sales.order-placed.v1", json =>
        {
            var message = JsonSerializer.Deserialize<OrderPlacedMessage>(json)!;
            var shipmentId = $"SHP-{message.OrderId}";
            Shipments.Add($"{shipmentId} (Pending, {message.Skus.Length} items)");
            Console.WriteLine($"  [Shipping] created {shipmentId} for {message.OrderId}");
        });
    }
}

namespace BillingContext
{
    sealed record OrderPlacedMessage(string OrderId, string CustomerId, decimal TotalAmount, string Currency);

    sealed class InvoiceHandler
    {
        public List<string> Invoices { get; } = [];

        public InvoiceHandler(InMemoryMessageBus bus) => bus.Subscribe("sales.order-placed.v1", json =>
        {
            var message = JsonSerializer.Deserialize<OrderPlacedMessage>(json)!;
            var invoiceId = $"INV-{message.OrderId}";
            Invoices.Add($"{invoiceId} ({message.TotalAmount} {message.Currency} for {message.CustomerId})");
            Console.WriteLine($"  [Billing]  created {invoiceId}, {message.TotalAmount} {message.Currency}");
        });
    }
}
