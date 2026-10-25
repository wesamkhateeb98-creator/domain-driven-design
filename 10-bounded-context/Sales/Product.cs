namespace BoundedContext.Sales;

// "Product" in Sales = something we sell at a price
public sealed record Product(string ProductId, string Name, decimal PriceUsd, decimal? DiscountPercent)
{
    public decimal FinalPrice => DiscountPercent is { } d ? decimal.Round(PriceUsd * (1 - d / 100), 2) : PriceUsd;
}
