namespace BoundedContext.Shipping;

// "Product" in Shipping = a box with weight and handling needs. No price here.
public sealed record Product(string ProductId, decimal WeightKg, bool IsFragile)
{
    private const decimal BaseFee = 5;
    private const decimal PerKg = 2;
    private const decimal FragileFee = 10;

    public decimal ShippingCost => BaseFee + WeightKg * PerKg + (IsFragile ? FragileFee : 0);
}
