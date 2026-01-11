using Orleans;


namespace Mississippi.EventSourcing.UxProjections.Abstractions.Subscriptions;

/// <summary>
///     Request to subscribe to a projection for real-time version change notifications.
/// </summary>
/// <remarks>
///     <para>
///         Subscription requests contain all the information needed to identify a specific projection
///         and route version change notifications back to the requesting client.
///     </para>
///     <para>
///         The <see cref="ClientSubscriptionId" /> is assigned by the client to enable matching
///         responses with the original subscription request.
///     </para>
/// </remarks>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.UxProjections.Abstractions.UxProjectionSubscriptionRequest")]
public sealed record UxProjectionSubscriptionRequest
{
    /// <summary>
    ///     Gets the brook type that the projection consumes.
    /// </summary>
    [Id(1)]
    public required string BrookType { get; init; }

    /// <summary>
    ///     Gets the client-assigned subscription identifier for matching responses.
    /// </summary>
    [Id(3)]
    public required string ClientSubscriptionId { get; init; }

    /// <summary>
    ///     Gets the entity identifier within the brook.
    /// </summary>
    [Id(2)]
    public required string EntityId { get; init; }

    /// <summary>
    ///     Gets the name of the projection type to subscribe to.
    /// </summary>
    [Id(0)]
    public required string ProjectionType { get; init; }
}