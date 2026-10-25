using ContextMap.Catalog;
using ContextMap.SharedKernel;

namespace ContextMap.Sales;

// Conformist: Sales uses the upstream ProductDtoV1 AS-IS, no translation of its own
public sealed class QuoteService(CatalogApi catalog)
{
    public decimal Quote(ProductId productId, int quantity)
    {
        ProductDtoV1 product = catalog.GetProduct(productId.Value);
        return product.PriceUsd * quantity;
    }
}
