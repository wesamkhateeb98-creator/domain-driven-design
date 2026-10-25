Console.WriteLine("✅ Value object: equal by value, immutable, always valid");
var a = Money.Of(10, "USD");
var b = Money.Of(10, "USD");
Show("a = Money.Of(10, USD)", a);
Show("b = Money.Of(10, USD)", b);
Show("a == b", a == b);
Show("ReferenceEquals(a, b)", ReferenceEquals(a, b));

var sum = a.Add(Money.Of(5.5m, "USD"));
Show("a.Add(5.50 USD)", sum);
Show("a after Add (unchanged)", a);
Show("a.Multiply(3)", a.Multiply(3));
Try("Money.Of(-5, USD)", () => Money.Of(-5, "USD"));
Try("Money.Of(5, XYZ)", () => Money.Of(5, "XYZ"));
Try("a.Add(Money.Of(10, EUR))", () => a.Add(Money.Of(10, "EUR")));

Console.WriteLine();
Console.WriteLine("❌ Primitive obsession: decimal has no currency and no rules");
decimal priceUsd = 10;
decimal priceEur = 10;
Show("priceUsd + priceEur", $"{priceUsd + priceEur}  (20 what?)");
decimal negative = -5;
Show("decimal negative = -5", $"accepted, {negative}");

static void Show(string label, object? value) => Console.WriteLine($"  {label,-28}: {value}");

static void Try(string label, Func<object> action)
{
    try { Show(label, $"accepted, {action()}"); }
    catch (ArgumentException ex) { Show(label, $"rejected -> {ex.Message}"); }
}

sealed record Money
{
    private static readonly string[] KnownCurrencies = ["USD", "EUR", "SAR"];

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }
    public string Currency { get; }

    public static Money Of(decimal amount, string currency)
    {
        if (amount < 0)
            throw new ArgumentException("money cannot be negative");
        if (!KnownCurrencies.Contains(currency))
            throw new ArgumentException($"unknown currency '{currency}'");
        return new Money(decimal.Round(amount, 2), currency);
    }

    public Money Add(Money other)
    {
        if (other.Currency != Currency)
            throw new ArgumentException($"cannot add {other.Currency} to {Currency}");
        return Of(Amount + other.Amount, Currency);
    }

    public Money Multiply(int quantity) => Of(Amount * quantity, Currency);

    public override string ToString() => $"{Amount:0.00} {Currency}";
}
