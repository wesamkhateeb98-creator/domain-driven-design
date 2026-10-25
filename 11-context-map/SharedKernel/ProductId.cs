namespace ContextMap.SharedKernel;

// Shared Kernel: a tiny model that two teams own TOGETHER. Change it only by agreement.
public readonly record struct ProductId(string Value)
{
    public override string ToString() => Value;
}
