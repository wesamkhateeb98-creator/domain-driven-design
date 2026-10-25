using CapstoneShop.SharedKernel;

namespace CapstoneShop.Sales;

public readonly record struct OrderId(string Value)
{
    public override string ToString() => Value;
}

public enum OrderStatus { Placed, Paid, Shipped, Cancelled }

public sealed record OrderLine(string Sku, Money UnitPrice, int Quantity)
{
    public Money Subtotal => UnitPrice.Multiply(Quantity);
}

public sealed record OrderPlaced(OrderId OrderId, string CustomerId, Money Total) : IDomainEvent;
public sealed record OrderPaid(OrderId OrderId) : IDomainEvent;
public sealed record OrderShipped(OrderId OrderId, string ShipmentId) : IDomainEvent;
public sealed record OrderCancelled(OrderId OrderId, string Reason) : IDomainEvent;

// Aggregate root. Invariants: 1..5 lines, total <= 5,000 USD, legal status transitions only
public sealed class Order : AggregateRoot<OrderId>
{
    public const int MaxLines = 5;
    public static readonly Money MaxTotal = Money.Usd(5000);

    private readonly List<OrderLine> _lines;

    private Order(OrderId id, string customerId, List<OrderLine> lines, Money discount) : base(id)
    {
        CustomerId = customerId;
        _lines = lines;
        Discount = discount;
    }

    public string CustomerId { get; }
    public OrderStatus Status { get; private set; } = OrderStatus.Placed;
    public IReadOnlyList<OrderLine> Lines => _lines.AsReadOnly();
    public Money Discount { get; }
    public Money Total => _lines.Aggregate(Money.Zero, (sum, l) => sum.Add(l.Subtotal)).Subtract(Discount);

    public static Order Place(OrderId id, string customerId, List<OrderLine> lines, Money discount)
    {
        if (lines.Count is 0 or > MaxLines)
            throw new DomainException($"an order needs 1..{MaxLines} lines");

        var order = new Order(id, customerId, lines, discount);
        if (order.Total.IsGreaterThan(MaxTotal))
            throw new DomainException($"total {order.Total} exceeds {MaxTotal}");

        order.Raise(new OrderPlaced(id, customerId, order.Total));
        return order;
    }

    public void MarkPaid()
    {
        Ensure(OrderStatus.Placed);
        Status = OrderStatus.Paid;
        Raise(new OrderPaid(Id));
    }

    public void MarkShipped(string shipmentId)
    {
        Ensure(OrderStatus.Paid);
        Status = OrderStatus.Shipped;
        Raise(new OrderShipped(Id, shipmentId));
    }

    public void Cancel(string reason)
    {
        Ensure(OrderStatus.Placed);
        Status = OrderStatus.Cancelled;
        Raise(new OrderCancelled(Id, reason));
    }

    private void Ensure(OrderStatus expected)
    {
        if (Status != expected)
            throw new DomainException($"order {Id} is {Status}, expected {expected}");
    }
}
