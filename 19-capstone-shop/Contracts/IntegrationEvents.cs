namespace CapstoneShop.Contracts;

// Published Language between contexts: primitives only, versioned
public sealed record OrderPlacedV1(string OrderId, string CustomerId, decimal Total);
public sealed record PaymentSucceededV1(string OrderId, decimal Amount);
public sealed record PaymentFailedV1(string OrderId, string Reason);
public sealed record ShipmentCreatedV1(string OrderId, string ShipmentId);
