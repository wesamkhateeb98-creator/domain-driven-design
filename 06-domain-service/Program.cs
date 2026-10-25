// Pricing needs Customer + Cart + Coupon at once.
// It belongs to none of them, so it lives in a stateless domain service.

var pricing = new PricingService();

var sara = new Customer("Sara", CustomerTier.Regular);
var omar = new Customer("Omar", CustomerTier.Vip);

Print("Sara (Regular), cart 300", pricing.Quote(sara, new Cart([new("KB", 100, 3)]), coupon: null));
Print("Omar (VIP), cart 600", pricing.Quote(omar, new Cart([new("MONITOR", 600, 1)]), coupon: null));
Print("Omar (VIP), cart 550 + WELCOME50", pricing.Quote(omar, new Cart([new("KB", 100, 1), new("MOUSE", 25, 18)]), Coupon.Welcome50));

static void Print(string title, PriceQuote q)
{
    Console.WriteLine(title);
    Console.WriteLine($"  {"subtotal",-14}: {q.Subtotal:0.00}");
    Console.WriteLine($"  {"vip discount",-14}: -{q.VipDiscount:0.00}");
    Console.WriteLine($"  {"coupon",-14}: -{q.CouponDiscount:0.00}");
    Console.WriteLine($"  {"shipping",-14}: {q.Shipping:0.00}");
    Console.WriteLine($"  {"total",-14}: {q.Total:0.00}");
    Console.WriteLine();
}

enum CustomerTier { Regular, Vip }

sealed record Customer(string Name, CustomerTier Tier);
sealed record CartItem(string Sku, decimal UnitPrice, int Quantity);
sealed record Cart(IReadOnlyList<CartItem> Items)
{
    public decimal Subtotal => Items.Sum(i => i.UnitPrice * i.Quantity);
}

sealed record Coupon(string Code, decimal Amount)
{
    public static readonly Coupon Welcome50 = new("WELCOME50", 50);
}

sealed record PriceQuote(decimal Subtotal, decimal VipDiscount, decimal CouponDiscount, decimal Shipping)
{
    public decimal Total => Subtotal - VipDiscount - CouponDiscount + Shipping;
}

// Domain service: named in the ubiquitous language, stateless, pure domain logic
sealed class PricingService
{
    private const decimal VipRate = 0.10m;
    private const decimal FreeShippingFrom = 500;
    private const decimal ShippingFee = 20;

    public PriceQuote Quote(Customer customer, Cart cart, Coupon? coupon)
    {
        var subtotal = cart.Subtotal;
        var vipDiscount = customer.Tier == CustomerTier.Vip ? decimal.Round(subtotal * VipRate, 2) : 0;
        var couponDiscount = coupon?.Amount ?? 0;
        var afterDiscounts = subtotal - vipDiscount - couponDiscount;
        var shipping = afterDiscounts >= FreeShippingFrom ? 0 : ShippingFee;
        return new PriceQuote(subtotal, vipDiscount, couponDiscount, shipping);
    }
}
