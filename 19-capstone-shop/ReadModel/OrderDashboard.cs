using CapstoneShop.Contracts;
using CapstoneShop.Infrastructure;

namespace CapstoneShop.ReadModel;

public sealed record OrderRow(string OrderId, string CustomerId, decimal Total, string Status, string? ShipmentId);

// CQRS read side: built only from integration events, shaped for one screen
public sealed class OrderDashboard
{
    private readonly Dictionary<string, OrderRow> _rows = [];

    public OrderDashboard(MessageBus bus)
    {
        bus.Subscribe<OrderPlacedV1>((_, e) => _rows[e.OrderId] = new OrderRow(e.OrderId, e.CustomerId, e.Total, "Placed", null));
        bus.Subscribe<PaymentSucceededV1>((_, e) => _rows[e.OrderId] = _rows[e.OrderId] with { Status = "Paid" });
        bus.Subscribe<PaymentFailedV1>((_, e) => _rows[e.OrderId] = _rows[e.OrderId] with { Status = "Cancelled" });
        bus.Subscribe<ShipmentCreatedV1>((_, e) => _rows[e.OrderId] = _rows[e.OrderId] with { Status = "Shipped", ShipmentId = e.ShipmentId });
    }

    public IEnumerable<OrderRow> All() => _rows.Values.OrderBy(r => r.OrderId);
}
