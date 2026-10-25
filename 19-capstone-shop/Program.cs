using CapstoneShop.Billing;
using CapstoneShop.Infrastructure;
using CapstoneShop.ReadModel;
using CapstoneShop.Sales;
using CapstoneShop.SharedKernel;
using CapstoneShop.Shipping;

var bus = new MessageBus();
var catalog = new Dictionary<string, Money>
{
    ["KB"] = Money.Usd(100),
    ["MOUSE"] = Money.Usd(25),
    ["MONITOR"] = Money.Usd(1500),
};

var sales = new SalesModule(new InMemoryOrderRepository(), new PricingService(catalog), bus);
var billing = new BillingModule([new Wallet("sara", 1000), new Wallet("omar", 1000)], bus);
var shipping = new ShippingModule(bus);
var dashboard = new OrderDashboard(bus);
Outbox[] outboxes = [sales.Outbox, billing.Outbox, shipping.Outbox];

Step("1. Sara (Regular) orders 2 KB + 4 MOUSE", () =>
    sales.PlaceOrder(new PlaceOrderCommand("ORD-1", "sara", CustomerTier.Regular, [new("KB", 2), new("MOUSE", 4)])));

Step("2. Omar (VIP) orders 1 MONITOR", () =>
    sales.PlaceOrder(new PlaceOrderCommand("ORD-2", "omar", CustomerTier.Vip, [new("MONITOR", 1)])));

Step("3. Sara orders 4 MONITOR (over the 5,000 USD limit)", () =>
    sales.PlaceOrder(new PlaceOrderCommand("ORD-3", "sara", CustomerTier.Regular, [new("MONITOR", 4)])));

Console.WriteLine("Dashboard (read model)");
foreach (var row in dashboard.All())
    Console.WriteLine($"  {row.OrderId} | {row.CustomerId,-4} | {row.Total,8} | {row.Status,-9} | {row.ShipmentId ?? "-"}");
Console.WriteLine($"  wallets: sara={billing.BalanceOf("sara")}, omar={billing.BalanceOf("omar")}; shipments={shipping.Shipments.Count}");

void Step(string title, Action command)
{
    Console.WriteLine(title);
    try
    {
        command();
    }
    catch (DomainException ex)
    {
        Console.WriteLine($"  [Sales]    rejected -> {ex.Message}");
    }

    // Relay outboxes + deliver messages until the system is quiet (eventual consistency)
    int moved;
    do
    {
        moved = outboxes.Sum(o => o.Relay(bus)) + bus.DeliverPending();
    } while (moved > 0);
    Console.WriteLine();
}
