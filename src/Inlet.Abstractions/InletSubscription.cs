using Orleans;


namespace Mississippi.Inlet.Abstractions;

/// <summary>
///     Represents an active subscription to a projection.
/// </summary>
/// <param name="SubscriptionId">The unique subscription identifier.</param>
/// <param name="ProjectionType">The type name of the projection.</param>
/// <param name="EntityId">The entity identifier.</param>
[GenerateSerializer]
[Alias("Mississippi.Inlet.Abstractions.InletSubscription")]
public sealed record InletSubscription(
    [property: Id(0)] string SubscriptionId,
    [property: Id(1)] string ProjectionType,
    [property: Id(2)] string EntityId
);