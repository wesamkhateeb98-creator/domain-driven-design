var now = new DateTime(2026, 10, 25, 10, 0, 0, DateTimeKind.Utc);
var dispatcher = new DomainEventDispatcher();
var loyalty = new Dictionary<string, int>();

dispatcher.Subscribe<OrderPlaced>(e =>
    Console.WriteLine($"  [EmailHandler]   send 'Order {e.OrderId} confirmed, {e.Total} USD' to {e.CustomerEmail}"));
dispatcher.Subscribe<OrderPlaced>(e =>
{
    var points = (int)(e.Total / 10);
    loyalty[e.CustomerEmail] = loyalty.GetValueOrDefault(e.CustomerEmail) + points;
    Console.WriteLine($"  [LoyaltyHandler] +{points} points for {e.CustomerEmail} (1 point per 10 USD)");
});
dispatcher.Subscribe<OrderCancelled>(e =>
    Console.WriteLine($"  [StockHandler]   release stock of order {e.OrderId}, reason '{e.Reason}'"));

var order = Order.Create("ORD-1", "sara@shop.com");
order.AddLine(100, 2);
order.AddLine(25, 3);
order.Place(now);
Show("order.Place()", $"{order.DomainEvents.Count} pending event: {order.DomainEvents[0].GetType().Name}");

Console.WriteLine("  repository.Save(order)  -> then dispatch:");
dispatcher.DispatchAndClear(order);
Show("after dispatch", $"{order.DomainEvents.Count} pending events");

order.Cancel("customer changed mind", now.AddMinutes(5));
dispatcher.DispatchAndClear(order);
Show("loyalty[sara]", loyalty["sara@shop.com"]);

static void Show(string label, object? value) => Console.WriteLine($"  {label,-16}: {value}");

interface IDomainEvent
{
    DateTime OccurredAt { get; }
}

// Past tense: something that already happened in the domain
sealed record OrderPlaced(string OrderId, string CustomerEmail, decimal Total, DateTime OccurredAt) : IDomainEvent;
sealed record OrderCancelled(string OrderId, string Reason, DateTime OccurredAt) : IDomainEvent;

abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}

sealed class Order : AggregateRoot
{
    private readonly List<decimal> _lineTotals = [];
    private bool _placed;

    private Order(string id, string customerEmail)
    {
        Id = id;
        CustomerEmail = customerEmail;
    }

    public string Id { get; }
    public string CustomerEmail { get; }
    public decimal Total => _lineTotals.Sum();

    public static Order Create(string id, string customerEmail) => new(id, customerEmail);

    public void AddLine(decimal unitPrice, int quantity) => _lineTotals.Add(unitPrice * quantity);

    public void Place(DateTime now)
    {
        _placed = true;
        Raise(new OrderPlaced(Id, CustomerEmail, Total, now));
    }

    public void Cancel(string reason, DateTime now)
    {
        if (!_placed)
            throw new InvalidOperationException("only a placed order can be cancelled");
        Raise(new OrderCancelled(Id, reason, now));
    }
}

sealed class DomainEventDispatcher
{
    private readonly Dictionary<Type, List<Action<IDomainEvent>>> _handlers = [];

    public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IDomainEvent
    {
        if (!_handlers.TryGetValue(typeof(TEvent), out var list))
            _handlers[typeof(TEvent)] = list = [];
        list.Add(e => handler((TEvent)e));
    }

    public void DispatchAndClear(AggregateRoot aggregate)
    {
        var events = aggregate.DomainEvents.ToList();
        aggregate.ClearDomainEvents();
        foreach (var domainEvent in events)
            foreach (var handler in _handlers.GetValueOrDefault(domainEvent.GetType()) ?? [])
                handler(domainEvent);
    }
}
