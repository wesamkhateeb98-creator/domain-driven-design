namespace CapstoneShop.SharedKernel;

public sealed record Money
{
    private Money(decimal amount) => Amount = amount;

    public decimal Amount { get; }

    public static readonly Money Zero = new(0);

    public static Money Usd(decimal amount)
    {
        if (amount < 0)
            throw new DomainException("money cannot be negative");
        return new Money(decimal.Round(amount, 2));
    }

    public Money Add(Money other) => Usd(Amount + other.Amount);
    public Money Subtract(Money other) => Usd(Amount - other.Amount);
    public Money Multiply(decimal factor) => Usd(Amount * factor);
    public bool IsGreaterThan(Money other) => Amount > other.Amount;

    public override string ToString() => $"{Amount:0.00} USD";
}
