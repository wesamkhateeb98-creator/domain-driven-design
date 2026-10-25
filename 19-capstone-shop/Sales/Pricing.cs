using CapstoneShop.SharedKernel;

namespace CapstoneShop.Sales;

public enum CustomerTier { Regular, Vip }

public sealed record CartItem(string Sku, int Quantity);

// Domain service: needs the catalog prices AND the customer tier
public sealed class PricingService(IReadOnlyDictionary<string, Money> catalog)
{
    private const decimal VipRate = 0.10m;

    public (List<OrderLine> Lines, Money Discount) Price(IEnumerable<CartItem> cart, CustomerTier tier)
    {
        var lines = cart.Select(item => new OrderLine(
            item.Sku,
            catalog.TryGetValue(item.Sku, out var price) ? price : throw new DomainException($"unknown product '{item.Sku}'"),
            item.Quantity)).ToList();

        var subtotal = lines.Aggregate(Money.Zero, (sum, l) => sum.Add(l.Subtotal));
        var discount = tier == CustomerTier.Vip ? subtotal.Multiply(VipRate) : Money.Zero;
        return (lines, discount);
    }
}
