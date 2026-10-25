namespace CapstoneShop.SharedKernel;

public interface IDomainEvent;

public sealed class DomainException(string message) : Exception(message);

public abstract class AggregateRoot<TId>(TId id) where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public TId Id { get; } = id;
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public IReadOnlyList<IDomainEvent> PullDomainEvents()
    {
        var events = _domainEvents.ToList();
        _domainEvents.Clear();
        return events;
    }
}
