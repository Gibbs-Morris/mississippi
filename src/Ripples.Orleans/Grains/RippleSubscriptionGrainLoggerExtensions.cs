using System;

using Microsoft.Extensions.Logging;


namespace Mississippi.Ripples.Orleans.Grains;

/// <summary>
///     High-performance logging extensions for <see cref="RippleSubscriptionGrain" />.
/// </summary>
internal static partial class RippleSubscriptionGrainLoggerExtensions
{
    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Information,
        Message = "All subscriptions cleared for connection {ConnectionId}")]
    public static partial void AllSubscriptionsCleared(
        this ILogger logger,
        string connectionId
    );

    [LoggerMessage(
        EventId = 15,
        Level = LogLevel.Debug,
        Message = "Brook stream completed for connection {ConnectionId}")]
    public static partial void BrookStreamCompleted(
        this ILogger logger,
        string connectionId
    );

    [LoggerMessage(EventId = 16, Level = LogLevel.Error, Message = "Brook stream error for connection {ConnectionId}")]
    public static partial void BrookStreamError(
        this ILogger logger,
        string connectionId,
        Exception exception
    );

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Debug,
        Message = "Clearing all {Count} subscriptions for connection {ConnectionId}")]
    public static partial void ClearingAllSubscriptions(
        this ILogger logger,
        string connectionId,
        int count
    );

    [LoggerMessage(
        EventId = 13,
        Level = LogLevel.Error,
        Message =
            "Failed to send notification for projection {ProjectionType}/{EntityId} for connection {ConnectionId}")]
    public static partial void FailedToSendNotification(
        this ILogger logger,
        string connectionId,
        string projectionType,
        string entityId,
        Exception exception
    );

    [LoggerMessage(
        EventId = 11,
        Level = LogLevel.Error,
        Message = "Failed to unsubscribe from brook stream {BrookKey} for connection {ConnectionId}")]
    public static partial void FailedToUnsubscribeFromBrookStream(
        this ILogger logger,
        string connectionId,
        string brookKey,
        Exception exception
    );

    [LoggerMessage(
        EventId = 12,
        Level = LogLevel.Debug,
        Message =
            "Notification sent for projection {ProjectionType}/{EntityId} at version {Version} for connection {ConnectionId}")]
    public static partial void NotificationSent(
        this ILogger logger,
        string connectionId,
        string projectionType,
        string entityId,
        long version
    );

    [LoggerMessage(
        EventId = 14,
        Level = LogLevel.Warning,
        Message =
            "Projection type {ProjectionType} is not registered in the projection brook registry for connection {ConnectionId}")]
    public static partial void ProjectionTypeNotRegistered(
        this ILogger logger,
        string connectionId,
        string projectionType
    );

    [LoggerMessage(
        EventId = 9,
        Level = LogLevel.Debug,
        Message = "Subscribed to brook stream {BrookKey} at position {Position} for connection {ConnectionId}")]
    public static partial void SubscribedToBrookStream(
        this ILogger logger,
        string connectionId,
        string brookKey,
        long position
    );

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Information,
        Message =
            "Subscribed to projection {ProjectionType}/{EntityId} with subscriptionId {SubscriptionId} for connection {ConnectionId}")]
    public static partial void SubscribedToProjection(
        this ILogger logger,
        string connectionId,
        string subscriptionId,
        string projectionType,
        string entityId
    );

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message =
            "Subscribing to projection {ProjectionType}/{EntityId} with subscriptionId {SubscriptionId} on brook {BrookName} for connection {ConnectionId}")]
    public static partial void SubscribingToProjection(
        this ILogger logger,
        string connectionId,
        string subscriptionId,
        string projectionType,
        string entityId,
        string brookName
    );

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Subscription grain activated for connection {ConnectionId}")]
    public static partial void SubscriptionGrainActivated(
        this ILogger logger,
        string connectionId
    );

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Warning,
        Message = "Subscription {SubscriptionId} not found for connection {ConnectionId}")]
    public static partial void SubscriptionNotFound(
        this ILogger logger,
        string connectionId,
        string subscriptionId
    );

    [LoggerMessage(
        EventId = 10,
        Level = LogLevel.Debug,
        Message = "Unsubscribed from brook stream {BrookKey} for connection {ConnectionId}")]
    public static partial void UnsubscribedFromBrookStream(
        this ILogger logger,
        string connectionId,
        string brookKey
    );

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Information,
        Message = "Unsubscribed from projection with subscriptionId {SubscriptionId} for connection {ConnectionId}")]
    public static partial void UnsubscribedFromProjection(
        this ILogger logger,
        string connectionId,
        string subscriptionId
    );

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Debug,
        Message =
            "Unsubscribing from projection {ProjectionType}/{EntityId} with subscriptionId {SubscriptionId} for connection {ConnectionId}")]
    public static partial void UnsubscribingFromProjection(
        this ILogger logger,
        string connectionId,
        string subscriptionId,
        string projectionType,
        string entityId
    );
}