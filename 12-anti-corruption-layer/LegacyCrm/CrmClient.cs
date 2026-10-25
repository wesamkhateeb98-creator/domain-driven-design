namespace AntiCorruptionLayer.LegacyCrm;

// External legacy system: we cannot change it. Cryptic codes, cents, upper case, one flat string.
public sealed class CrmClient
{
    private readonly Dictionary<int, string> _records = new()
    {
        [501] = "cust_nm=ALI HASSAN;cust_tp=V;bal_cents=125050;stat=A",
        [502] = "cust_nm=LINA SAAD;cust_tp=R;bal_cents=990;stat=I",
        [503] = "cust_nm=JOHN DOE;cust_tp=Q;bal_cents=0;stat=A",
    };

    public string FetchCustomerRecord(int custNo) => _records[custNo];
}
