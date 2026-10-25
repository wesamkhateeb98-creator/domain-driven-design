using SalesProduct = BoundedContext.Sales.Product;
using ShippingProduct = BoundedContext.Shipping.Product;

// Same id "P-1", two different models, each correct inside its own context
SalesProduct forSales = new BoundedContext.Sales.SalesCatalog().Get("P-1");
ShippingProduct forShipping = new BoundedContext.Shipping.ShippingCatalog().Get("P-1");

Console.WriteLine("Sales context: Product = something we sell");
Show("name", forSales.Name);
Show("price", $"{forSales.PriceUsd} USD");
Show("discount", $"{forSales.DiscountPercent}%");
Show("final price", $"{forSales.FinalPrice} USD");

Console.WriteLine();
Console.WriteLine("Shipping context: Product = a box to deliver");
Show("weight", $"{forShipping.WeightKg} kg");
Show("fragile", forShipping.IsFragile);
Show("shipping cost", $"5 + {forShipping.WeightKg} x 2 + 10 = {forShipping.ShippingCost} USD");

Console.WriteLine();
Console.WriteLine("Shared between contexts: only the id");
Show("forSales.ProductId", forSales.ProductId);
Show("forShipping.ProductId", forShipping.ProductId);

static void Show(string label, object? value) => Console.WriteLine($"  {label,-22}: {value}");
