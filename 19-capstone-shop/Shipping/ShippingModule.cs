using CapstoneShop.Contracts;
using CapstoneShop.Infrastructure;

namespace CapstoneShop.Shipping;

public sealed record Shipment(string ShipmentId, string OrderId, string Status);

public sealed class ShippingModule
{
    private readonly List<Shipment> _shipments = [];
    private readonly Inbox _inbox = new();

    public ShippingModule(MessageBus bus) => bus.Subscribe<PaymentSucceededV1>(OnPaymentSucceeded);

    public Outbox Outbox { get; } = new("shipping");
    public IReadOnlyList<Shipment> Shipments => _shipments.AsReadOnly();

    private void OnPaymentSucceeded(Guid messageId, PaymentSucceededV1 e)
    {
        if (!_inbox.TryAccept(messageId))
            return;

        var shipment = new Shipment($"SHP-{_shipments.Count + 1:000}", e.OrderId, "ReadyToShip");
        _shipments.Add(shipment);
        Console.WriteLine($"  [Shipping] {shipment.ShipmentId} for {e.OrderId}");
        Outbox.Add(new ShipmentCreatedV1(e.OrderId, shipment.ShipmentId));
    }
}
