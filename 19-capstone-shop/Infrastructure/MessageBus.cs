using System.Text.Json;

namespace CapstoneShop.Infrastructure;

public sealed record Envelope(Guid MessageId, string Topic, string Json);

// Asynchronous in spirit: Publish only enqueues; DeliverPending() runs the subscribers later
public sealed class MessageBus
{
    private readonly Queue<Envelope> _queue = new();
    private readonly Dictionary<string, List<Action<Envelope>>> _subscribers = [];

    public void Publish(Envelope envelope) => _queue.Enqueue(envelope);

    public void Subscribe<T>(Action<Guid, T> handler)
    {
        var topic = typeof(T).Name;
        if (!_subscribers.TryGetValue(topic, out var list))
            _subscribers[topic] = list = [];
        list.Add(e => handler(e.MessageId, JsonSerializer.Deserialize<T>(e.Json)!));
    }

    public int DeliverPending()
    {
        var delivered = 0;
        while (_queue.TryDequeue(out var envelope))
        {
            Console.WriteLine($"    [bus] {envelope.Topic} {envelope.Json}");
            foreach (var subscriber in _subscribers.GetValueOrDefault(envelope.Topic) ?? [])
                subscriber(envelope);
            delivered++;
        }
        return delivered;
    }
}
