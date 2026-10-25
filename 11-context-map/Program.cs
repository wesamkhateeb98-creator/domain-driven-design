using ContextMap.Catalog;
using ContextMap.Sales;
using ContextMap.SharedKernel;
using ContextMap.Shipping;

var catalog = new CatalogApi();                 // upstream (U)
var sales = new QuoteService(catalog);          // downstream (D), Conformist
var shipping = new ParcelService(catalog);      // downstream (D), Customer/Supplier

var monitor = new ProductId("P-1");             // Shared Kernel type used by Sales and Shipping

Console.WriteLine("Context map");
Show("Catalog  -> Sales", "Upstream/Downstream, Open Host Service + Published Language, Sales = Conformist");
Show("Catalog  -> Shipping", "Customer/Supplier (Shipping asked for WeightKg)");
Show("Sales <-> Shipping", "Shared Kernel (ProductId)");

Console.WriteLine();
Console.WriteLine("Calls across the map");
Show("sales.Quote(P-1, 2)", $"{sales.Quote(monitor, 2)} USD");
var parcel = shipping.Pack(monitor, 2);
Show("shipping.Pack(P-1, 2)", $"Parcel {parcel.ProductId}, {parcel.TotalWeightKg} kg");

static void Show(string label, object? value) => Console.WriteLine($"  {label,-22}: {value}");
