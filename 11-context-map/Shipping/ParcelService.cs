using ContextMap.Catalog;
using ContextMap.SharedKernel;

namespace ContextMap.Shipping;

// Customer/Supplier: Shipping (customer) asked Catalog (supplier) to add WeightKg to the contract,
// and Catalog planned it into its roadmap. Shipping keeps its own Parcel model.
public sealed record Parcel(ProductId ProductId, decimal TotalWeightKg);

public sealed class ParcelService(CatalogApi catalog)
{
    public Parcel Pack(ProductId productId, int quantity)
    {
        var product = catalog.GetProduct(productId.Value);
        return new Parcel(productId, product.WeightKg * quantity);
    }
}
