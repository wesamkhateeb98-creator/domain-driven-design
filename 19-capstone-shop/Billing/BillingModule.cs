using CapstoneShop.Contracts;
using CapstoneShop.Infrastructure;

namespace CapstoneShop.Billing;

// Billing's own model of a customer: just a wallet. It knows nothing about orders' lines.
public sealed class Wallet(string customerId, decimal balance)
{
    public string CustomerId { get; } = customerId;
    public decimal Balance { get; private set; } = balance;

    public bool TryCharge(decimal amount)
    {
        if (amount > Balance)
            return false;
        Balance -= amount;
        return true;
    }
}

public sealed class BillingModule
{
    private readonly Dictionary<string, Wallet> _wallets;
    private readonly Inbox _inbox = new();

    public BillingModule(IEnumerable<Wallet> wallets, MessageBus bus)
    {
        _wallets = wallets.ToDictionary(w => w.CustomerId);
        bus.Subscribe<OrderPlacedV1>(OnOrderPlaced);
    }

    public Outbox Outbox { get; } = new("billing");

    public decimal BalanceOf(string customerId) => _wallets[customerId].Balance;

    private void OnOrderPlaced(Guid messageId, OrderPlacedV1 e)
    {
        if (!_inbox.TryAccept(messageId))
            return;

        var wallet = _wallets[e.CustomerId];
        if (wallet.TryCharge(e.Total))
        {
            Console.WriteLine($"  [Billing]  charged {e.Total} to {e.CustomerId}, balance {wallet.Balance}");
            Outbox.Add(new PaymentSucceededV1(e.OrderId, e.Total));
        }
        else
        {
            var reason = $"insufficient funds ({e.Total} > {wallet.Balance})";
            Console.WriteLine($"  [Billing]  {reason}");
            Outbox.Add(new PaymentFailedV1(e.OrderId, reason));
        }
    }
}
