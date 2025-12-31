using System.Collections.Generic;

using Orleans;


namespace Mississippi.EventSourcing.UxProjections.Subscriptions;

/// <summary>
///     Persisted state for <see cref="UxProjectionSubscriptionGrain" />.
/// </summary>
/// <remarks>
///     Contains all active subscriptions for a single SignalR connection.
///     Stream subscription handles are not serialized; they are rehydrated on grain activation.
/// </remarks>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.UxProjections.UxProjectionSubscriptionState")]
internal sealed class UxProjectionSubscriptionState
{
    /// <summary>
    ///     Gets active subscriptions keyed by subscription ID.
    /// </summary>
    [Id(0)]
    public Dictionary<string, ActiveSubscription> Subscriptions { get; init; } = [];
}
