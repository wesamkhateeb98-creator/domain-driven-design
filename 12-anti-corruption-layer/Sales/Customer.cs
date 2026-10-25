namespace AntiCorruptionLayer.Sales;

// Our clean model, in OUR ubiquitous language
public enum CustomerTier { Regular, Vip }

public sealed record Customer(int Id, string Name, CustomerTier Tier, decimal BalanceUsd, bool IsActive);

// Port owned by Sales: Sales does not know a CRM exists
public interface ICustomerDirectory
{
    Customer Find(int customerId);
}
