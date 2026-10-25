namespace BoundedContext.Shipping;

public sealed class ShippingCatalog
{
    private readonly Dictionary<string, Product> _products = new()
    {
        ["P-1"] = new Product("P-1", WeightKg: 6.5m, IsFragile: true),
    };

    public Product Get(string productId) => _products[productId];
}
