using System.Collections.Immutable;
using System.Threading.Tasks;

using Mississippi.Inlet.Abstractions;

using Orleans;
using Orleans.Concurrency;


namespace Mississippi.Inlet.Orleans.Grains;

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
///         Clients subscribe by projection path and entity ID only. The grain resolves
///         the brook name from the <see cref="IProjectionBrookRegistry" />
///         and manages the underlying Orleans stream subscriptions.
///     </para>
///     <para>
///         When a brook cursor moves, the grain fans out notifications to all projection
///         subscriptions that share that brook, notifying SignalR clients with
///         (path, entityId, newVersion) - never exposing brook details.
///     </para>
/// </remarks>
[Alias("Mississippi.Inlet.Orleans.IInletSubscriptionGrain")]
public interface IInletSubscriptionGrain : IGrainWithStringKey
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
    Task<ImmutableList<InletSubscription>> GetSubscriptionsAsync();

    /// <summary>
    ///     Subscribes to a projection for version change notifications.
    /// </summary>
    /// <param name="path">The projection path (e.g., "cascade/channels").</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <returns>A server-assigned subscription identifier.</returns>
    /// <remarks>
    ///     The grain resolves the brook name internally from the registry.
    ///     The client never needs to know about brook details.
    /// </remarks>
    [Alias("SubscribeAsync")]
    Task<string> SubscribeAsync(
        string path,
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