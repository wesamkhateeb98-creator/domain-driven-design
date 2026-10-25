namespace BoundedContext.Sales;

public sealed class SalesCatalog
{
    private readonly Dictionary<string, Product> _products = new()
    {
        ["P-1"] = new Product("P-1", "Monitor 27\"", 1500, DiscountPercent: 10),
    };

    public Product Get(string productId) => _products[productId];
}
