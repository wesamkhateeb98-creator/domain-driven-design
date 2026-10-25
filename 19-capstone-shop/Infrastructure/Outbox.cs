using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace CapstoneShop.Infrastructure;

// One outbox per bounded context. Written in the same transaction as the aggregate.
public sealed class Outbox(string owner)
{
    private static readonly JsonSerializerOptions Json = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    private readonly List<(Envelope Envelope, bool Sent)> _rows = [];
    private int _sequence;

    public void Add<T>(T message) where T : notnull
    {
        var id = new Guid(MD5.HashData(Encoding.UTF8.GetBytes($"{owner}-{++_sequence}")));
        _rows.Add((new Envelope(id, typeof(T).Name, JsonSerializer.Serialize(message, Json)), false));
    }

    // Relay: push unsent rows to the bus, then mark them sent
    public int Relay(MessageBus bus)
    {
        var sent = 0;
        for (var i = 0; i < _rows.Count; i++)
        {
            if (_rows[i].Sent)
                continue;
            bus.Publish(_rows[i].Envelope);
            _rows[i] = (_rows[i].Envelope, true);
            sent++;
        }
        return sent;
    }
}

// Consumers remember handled message ids: delivery is at-least-once
public sealed class Inbox
{
    private readonly HashSet<Guid> _handled = [];

    public bool TryAccept(Guid messageId) => _handled.Add(messageId);
}
