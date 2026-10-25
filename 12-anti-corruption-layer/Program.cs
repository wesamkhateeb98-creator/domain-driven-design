using AntiCorruptionLayer.LegacyCrm;
using AntiCorruptionLayer.Sales;

var crm = new CrmClient();
ICustomerDirectory directory = new CrmCustomerTranslator(crm);

foreach (var id in new[] { 501, 502, 503 })
{
    Console.WriteLine($"Customer {id}");
    Show("legacy record", crm.FetchCustomerRecord(id));
    try
    {
        var c = directory.Find(id);
        Show("our model", $"Name={c.Name}, Tier={c.Tier}, Balance={c.BalanceUsd} USD, Active={c.IsActive}");
    }
    catch (InvalidOperationException ex)
    {
        Show("our model", $"rejected at the border -> {ex.Message}");
    }
    Console.WriteLine();
}

static void Show(string label, object? value) => Console.WriteLine($"  {label,-14}: {value}");
