using CapstoneShop.Contracts;
using CapstoneShop.Infrastructure;
using CapstoneShop.SharedKernel;

namespace CapstoneShop.Sales;

public sealed record PlaceOrderCommand(string OrderId, string CustomerId, CustomerTier Tier, List<CartItem> Cart);

public interface IOrderRepository
{
    Order Get(OrderId id);
    void Save(Order order);
}

public sealed class InMemoryOrderRepository : IOrderRepository
{
    private readonly Dictionary<OrderId, Order> _orders = [];
    public Order Get(OrderId id) => _orders[id];
    public void Save(Order order) => _orders[order.Id] = order;
}

// Application layer of Sales: one method per use case, no business rules here
public sealed class SalesModule
{
    private readonly IOrderRepository _orders;
    private readonly PricingService _pricing;
    private readonly Inbox _inbox = new();

    public SalesModule(IOrderRepository orders, PricingService pricing, MessageBus bus)
    {
        _orders = orders;
        _pricing = pricing;
        bus.Subscribe<PaymentSucceededV1>((id, e) => Handle(id, e.OrderId, o => o.MarkPaid()));
        bus.Subscribe<PaymentFailedV1>((id, e) => Handle(id, e.OrderId, o => o.Cancel(e.Reason)));
        bus.Subscribe<ShipmentCreatedV1>((id, e) => Handle(id, e.OrderId, o => o.MarkShipped(e.ShipmentId)));
    }

    public Outbox Outbox { get; } = new("sales");

    public void PlaceOrder(PlaceOrderCommand cmd)
    {
        var (lines, discount) = _pricing.Price(cmd.Cart, cmd.Tier);
        var order = Order.Place(new OrderId(cmd.OrderId), cmd.CustomerId, lines, discount);
        SaveWithEvents(order);
    }

    private void Handle(Guid messageId, string orderId, Action<Order> change)
    {
        if (!_inbox.TryAccept(messageId))
            return;
        var order = _orders.Get(new OrderId(orderId));
        change(order);
        SaveWithEvents(order);
    }

    // Same transaction: aggregate row + outbox rows
    private void SaveWithEvents(Order order)
    {
        _orders.Save(order);
        foreach (var domainEvent in order.PullDomainEvents())
        {
            Console.WriteLine($"  [Sales]    {domainEvent}");
            if (domainEvent is OrderPlaced placed)
                Outbox.Add(new OrderPlacedV1(placed.OrderId.Value, placed.CustomerId, placed.Total.Amount));
        }
    }
}
