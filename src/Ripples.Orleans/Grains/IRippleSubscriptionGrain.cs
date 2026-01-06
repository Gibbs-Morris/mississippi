using System.Collections.Immutable;
using System.Threading.Tasks;

using Orleans;
using Orleans.Concurrency;


namespace Mississippi.Ripples.Orleans.Grains;

/// <summary>
///     Grain managing all projection subscriptions for a single SignalR connection.
/// </summary>
/// <remarks>
///     <para>
///         This grain is keyed by SignalR <c>ConnectionId</c> and manages all projection
///         subscriptions for that connection. It handles brook stream subscriptions internally
///         with deduplication - multiple projections using the same brook and entity ID
///         share a single stream subscription.
///     </para>
///     <para>
///         Clients subscribe by projection type and entity ID only. The grain resolves
///         the brook name from the <see cref="Abstractions.IProjectionBrookRegistry" />
///         and manages the underlying Orleans stream subscriptions.
///     </para>
///     <para>
///         When a brook cursor moves, the grain fans out notifications to all projection
///         subscriptions that share that brook, notifying SignalR clients with
///         (projectionType, entityId, newVersion) - never exposing brook details.
///     </para>
/// </remarks>
[Alias("Mississippi.Ripples.Orleans.IRippleSubscriptionGrain")]
public interface IRippleSubscriptionGrain : IGrainWithStringKey
{
    /// <summary>
    ///     Clears all subscriptions for this connection.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    ///     This method should be called when the SignalR connection is closed to ensure
    ///     all subscriptions are properly cleaned up before the grain deactivates.
    /// </remarks>
    [Alias("ClearAllAsync")]
    Task ClearAllAsync();

    /// <summary>
    ///     Gets all active subscriptions for this connection.
    /// </summary>
    /// <returns>An immutable list of all active subscription details.</returns>
    [ReadOnly]
    [Alias("GetSubscriptionsAsync")]
    Task<ImmutableList<RippleSubscription>> GetSubscriptionsAsync();

    /// <summary>
    ///     Subscribes to a projection for version change notifications.
    /// </summary>
    /// <param name="projectionType">The projection type name.</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <returns>A server-assigned subscription identifier.</returns>
    /// <remarks>
    ///     The grain resolves the brook name internally from the registry.
    ///     The client never needs to know about brook details.
    /// </remarks>
    [Alias("SubscribeAsync")]
    Task<string> SubscribeAsync(
        string projectionType,
        string entityId
    );

    /// <summary>
    ///     Unsubscribes from a specific subscription.
    /// </summary>
    /// <param name="subscriptionId">The server-assigned subscription identifier.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Alias("UnsubscribeAsync")]
    Task UnsubscribeAsync(
        string subscriptionId
    );
}

/// <summary>
///     Represents an active projection subscription.
/// </summary>
/// <param name="SubscriptionId">The server-assigned subscription identifier.</param>
/// <param name="ProjectionType">The projection type name.</param>
/// <param name="EntityId">The entity identifier.</param>
[GenerateSerializer]
[Alias("Mississippi.Ripples.Orleans.RippleSubscription")]
public sealed record RippleSubscription(
    [property: Id(0)] string SubscriptionId,
    [property: Id(1)] string ProjectionType,
    [property: Id(2)] string EntityId
);