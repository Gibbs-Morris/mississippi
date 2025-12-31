using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.UxProjections.Abstractions;


namespace Mississippi.EventSourcing.UxProjections.Subscriptions;

/// <summary>
///     Logger extensions for <see cref="UxProjectionSubscriptionGrain" />.
/// </summary>
internal static partial class UxProjectionSubscriptionGrainLoggerExtensions
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "UX projection subscription grain activated for connection '{ConnectionId}'")]
    public static partial void SubscriptionGrainActivated(
        this ILogger logger,
        string connectionId
    );

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "Connection '{ConnectionId}' subscribing to projection with subscription ID '{SubscriptionId}' for '{ProjectionKey}'")]
    public static partial void SubscribingToProjection(
        this ILogger logger,
        string connectionId,
        string subscriptionId,
        UxProjectionKey projectionKey
    );

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Information,
        Message = "Connection '{ConnectionId}' subscribed to projection with subscription ID '{SubscriptionId}' for '{ProjectionKey}'")]
    public static partial void SubscribedToProjection(
        this ILogger logger,
        string connectionId,
        string subscriptionId,
        UxProjectionKey projectionKey
    );

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Debug,
        Message = "Connection '{ConnectionId}' unsubscribing from projection with subscription ID '{SubscriptionId}' for '{ProjectionKey}'")]
    public static partial void UnsubscribingFromProjection(
        this ILogger logger,
        string connectionId,
        string subscriptionId,
        UxProjectionKey projectionKey
    );

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Information,
        Message = "Connection '{ConnectionId}' unsubscribed from projection with subscription ID '{SubscriptionId}' for '{ProjectionKey}'")]
    public static partial void UnsubscribedFromProjection(
        this ILogger logger,
        string connectionId,
        string subscriptionId,
        UxProjectionKey projectionKey
    );

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Debug,
        Message = "Connection '{ConnectionId}' subscription ID '{SubscriptionId}' not found")]
    public static partial void SubscriptionNotFound(
        this ILogger logger,
        string connectionId,
        string subscriptionId
    );

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Debug,
        Message = "Connection '{ConnectionId}' clearing all {SubscriptionCount} subscriptions")]
    public static partial void ClearingAllSubscriptions(
        this ILogger logger,
        string connectionId,
        int subscriptionCount
    );

    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Information,
        Message = "Connection '{ConnectionId}' cleared all subscriptions")]
    public static partial void AllSubscriptionsCleared(
        this ILogger logger,
        string connectionId
    );
}
