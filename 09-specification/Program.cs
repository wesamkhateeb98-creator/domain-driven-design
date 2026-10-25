Customer[] customers =
[
    new("Sara", TotalSpent: 1500, UnpaidOrders: 0, Country: "SA"),
    new("Omar", TotalSpent: 2200, UnpaidOrders: 1, Country: "SA"),
    new("Lina", TotalSpent: 800, UnpaidOrders: 0, Country: "SA"),
    new("John", TotalSpent: 3000, UnpaidOrders: 0, Country: "UK"),
];

var isVip = new IsVipCustomer();
var hasUnpaid = new HasUnpaidOrders();
var livesInSaudi = new LivesIn("SA");

// Business sentence: "a free gift goes to VIP customers in Saudi Arabia with no unpaid orders"
var freeGift = isVip.And(hasUnpaid.Not()).And(livesInSaudi);

Console.WriteLine($"  {"customer",-8} | {"spent",5} | {"unpaid",6} | {"country",7} | {"VIP?",5} | {"free gift?",10}");
foreach (var c in customers)
    Console.WriteLine($"  {c.Name,-8} | {c.TotalSpent,5} | {c.UnpaidOrders,6} | {c.Country,7} | {isVip.IsSatisfiedBy(c),5} | {freeGift.IsSatisfiedBy(c),10}");

Console.WriteLine();
var winners = customers.Where(freeGift.IsSatisfiedBy).Select(c => c.Name);
Console.WriteLine($"  free gift winners: {string.Join(", ", winners)}");

sealed record Customer(string Name, decimal TotalSpent, int UnpaidOrders, string Country);

abstract class Specification<T>
{
    public abstract bool IsSatisfiedBy(T candidate);

    public Specification<T> And(Specification<T> other) => new AndSpecification<T>(this, other);
    public Specification<T> Or(Specification<T> other) => new OrSpecification<T>(this, other);
    public Specification<T> Not() => new NotSpecification<T>(this);
}

sealed class AndSpecification<T>(Specification<T> left, Specification<T> right) : Specification<T>
{
    public override bool IsSatisfiedBy(T c) => left.IsSatisfiedBy(c) && right.IsSatisfiedBy(c);
}

sealed class OrSpecification<T>(Specification<T> left, Specification<T> right) : Specification<T>
{
    public override bool IsSatisfiedBy(T c) => left.IsSatisfiedBy(c) || right.IsSatisfiedBy(c);
}

sealed class NotSpecification<T>(Specification<T> inner) : Specification<T>
{
    public override bool IsSatisfiedBy(T c) => !inner.IsSatisfiedBy(c);
}

// Each rule has one name from the ubiquitous language and lives in one place
sealed class IsVipCustomer : Specification<Customer>
{
    public const decimal Threshold = 1000;
    public override bool IsSatisfiedBy(Customer c) => c.TotalSpent > Threshold;
}

sealed class HasUnpaidOrders : Specification<Customer>
{
    public override bool IsSatisfiedBy(Customer c) => c.UnpaidOrders > 0;
}

sealed class LivesIn(string country) : Specification<Customer>
{
    public override bool IsSatisfiedBy(Customer c) => c.Country == country;
}
