// Core Domain Chart: score every subdomain, then decide where to invest.
//   differentiation (1-10): how much it makes customers pick US over competitors
//   complexity      (1-10): how hard the business rules are

Subdomain[] shop =
[
    new("Pricing & Promotions", Differentiation: 9, Complexity: 8),
    new("Recommendations", Differentiation: 8, Complexity: 9),
    new("Inventory", Differentiation: 5, Complexity: 6),
    new("Invoicing", Differentiation: 4, Complexity: 3),
    new("Payments", Differentiation: 2, Complexity: 8),
    new("Identity / Login", Differentiation: 1, Complexity: 7),
    new("Email sending", Differentiation: 1, Complexity: 2),
];

Console.WriteLine($"  {"subdomain",-22} | {"diff",4} | {"cplx",4} | {"type",-10} | strategy");
foreach (var s in shop.OrderByDescending(s => s.Differentiation))
    Console.WriteLine($"  {s.Name,-22} | {s.Differentiation,4} | {s.Complexity,4} | {s.Type,-10} | {s.Strategy}");

Console.WriteLine();
foreach (var group in shop.GroupBy(s => s.Type).OrderBy(g => g.Key))
    Console.WriteLine($"  {group.Key,-10}: {group.Count()} -> {string.Join(", ", group.Select(s => s.Name))}");

enum SubdomainType { Core, Supporting, Generic }

sealed record Subdomain(string Name, int Differentiation, int Complexity)
{
    public SubdomainType Type => Differentiation switch
    {
        >= 7 => SubdomainType.Core,
        <= 3 => SubdomainType.Generic,
        _ => SubdomainType.Supporting,
    };

    public string Strategy => Type switch
    {
        SubdomainType.Core => "build in-house, best people, full DDD",
        SubdomainType.Supporting => "build simple (CRUD is fine) or outsource",
        _ => "buy / use SaaS or a library",
    };
}
