namespace ContextMap.Catalog;

// Open Host Service: one public API for ALL downstream contexts (no custom endpoint per client)
public sealed class CatalogApi
{
    private readonly Dictionary<string, ProductDtoV1> _products = new()
    {
        ["P-1"] = new ProductDtoV1("P-1", "Monitor 27\"", 1500, 6.5m),
        ["P-2"] = new ProductDtoV1("P-2", "Keyboard", 100, 0.8m),
    };

    public ProductDtoV1 GetProduct(string productId) => _products[productId];
}
