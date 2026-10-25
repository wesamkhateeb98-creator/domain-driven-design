namespace ContextMap.Catalog;

// Published Language: the documented, versioned contract the Catalog publishes
public sealed record ProductDtoV1(string ProductId, string Name, decimal PriceUsd, decimal WeightKg);
