using System.Globalization;
using AntiCorruptionLayer.LegacyCrm;

namespace AntiCorruptionLayer.Sales;

// Anti-Corruption Layer: the ONLY place that knows the legacy format
public sealed class CrmCustomerTranslator(CrmClient crm) : ICustomerDirectory
{
    public Customer Find(int customerId)
    {
        var raw = crm.FetchCustomerRecord(customerId);
        var fields = raw.Split(';')
            .Select(pair => pair.Split('='))
            .ToDictionary(kv => kv[0], kv => kv[1]);

        return new Customer(
            Id: customerId,
            Name: CultureInfo.InvariantCulture.TextInfo.ToTitleCase(fields["cust_nm"].ToLowerInvariant()),
            Tier: TranslateTier(fields["cust_tp"]),
            BalanceUsd: int.Parse(fields["bal_cents"]) / 100m,
            IsActive: fields["stat"] == "A");
    }

    private static CustomerTier TranslateTier(string code) => code switch
    {
        "V" => CustomerTier.Vip,
        "R" => CustomerTier.Regular,
        _ => throw new InvalidOperationException($"unknown CRM tier code '{code}'"),
    };
}
