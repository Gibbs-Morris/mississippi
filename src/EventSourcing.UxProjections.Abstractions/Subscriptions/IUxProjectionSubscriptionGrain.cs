using System.Collections.Immutable;
using System.Threading.Tasks;

using Orleans;
using Orleans.Concurrency;


namespace Mississippi.EventSourcing.UxProjections.Abstractions.Subscriptions;

/// <summary>
///     Grain managing all projection subscriptions for a single SignalR connection.
/// </summary>
/// <remarks>
///     <para>
///         This grain is keyed by SignalR <c>ConnectionId</c> and manages all projection subscriptions
///         for that connection. It subscribes to projection version change streams and forwards
///         notifications to a per-connection output stream.
///     </para>
///     <para>
///         The grain is designed for one-grain-per-connection semantics to support multiple browser
///         windows per user, each with independent subscription lists.
///     </para>
///     <para>
///         On disconnect, callers should invoke <see cref="ClearAllAsync" /> to clean up subscriptions
///         before the grain deactivates.
///     </para>
/// </remarks>
[Alias("Mississippi.EventSourcing.UxProjections.Abstractions.IUxProjectionSubscriptionGrain")]
public interface IUxProjectionSubscriptionGrain : IGrainWithStringKey
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
    /// <returns>An immutable list of all active subscription requests.</returns>
    [ReadOnly]
    [Alias("GetSubscriptionsAsync")]
    Task<ImmutableList<UxProjectionSubscriptionRequest>> GetSubscriptionsAsync();

    /// <summary>
    ///     Subscribes to a projection for version change notifications.
    /// </summary>
    /// <param name="request">The subscription request containing projection details.</param>
    /// <returns>
    ///     A server-assigned subscription identifier that can be used to unsubscribe.
    /// </returns>
    [Alias("SubscribeAsync")]
    Task<string> SubscribeAsync(
        UxProjectionSubscriptionRequest request
    );

    /// <summary>
    ///     Unsubscribes from a specific subscription.
    /// </summary>
    /// <param name="subscriptionId">The server-assigned subscription identifier from <see cref="SubscribeAsync" />.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Alias("UnsubscribeAsync")]
    Task UnsubscribeAsync(
        string subscriptionId
    );
}