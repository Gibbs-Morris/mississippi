namespace Mississippi.Inlet.Abstractions;

/// <summary>
///     Represents an active subscription to a projection.
/// </summary>
/// <param name="SubscriptionId">The unique subscription identifier.</param>
/// <param name="ProjectionType">The type name of the projection.</param>
/// <param name="EntityId">The entity identifier.</param>
public sealed record InletSubscription(string SubscriptionId, string ProjectionType, string EntityId);