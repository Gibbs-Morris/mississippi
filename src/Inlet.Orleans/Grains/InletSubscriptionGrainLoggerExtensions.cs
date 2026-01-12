using System;

using Microsoft.Extensions.Logging;


namespace Mississippi.Inlet.Orleans.Grains;

/// <summary>
///     High-performance logging extensions for <see cref="InletSubscriptionGrain" />.
/// </summary>
internal static partial class InletSubscriptionGrainLoggerExtensions
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
        Message = "Failed to send notification for projection {Path}/{EntityId} for connection {ConnectionId}")]
    public static partial void FailedToSendNotification(
        this ILogger logger,
        string connectionId,
        string path,
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
        EventId = 19,
        Level = LogLevel.Debug,
        Message = "Found {SubscriptionCount} subscription(s) for brook '{BrookKey}' for connection {ConnectionId}")]
    public static partial void FoundBrookSubscriptions(
        this ILogger logger,
        string connectionId,
        string brookKey,
        int subscriptionCount
    );

    [LoggerMessage(
        EventId = 18,
        Level = LogLevel.Warning,
        Message =
            "No subscriptions found for brook '{BrookKey}' for connection {ConnectionId}. Total brooks tracked: {TotalBrooks}")]
    public static partial void NoBrookSubscriptionsFound(
        this ILogger logger,
        string connectionId,
        string brookKey,
        int totalBrooks
    );

    [LoggerMessage(
        EventId = 12,
        Level = LogLevel.Debug,
        Message =
            "Notification sent for projection {Path}/{EntityId} at version {Version} for connection {ConnectionId}")]
    public static partial void NotificationSent(
        this ILogger logger,
        string connectionId,
        string path,
        string entityId,
        long version
    );

    [LoggerMessage(
        EventId = 14,
        Level = LogLevel.Warning,
        Message =
            "Projection path {Path} is not registered in the projection brook registry for connection {ConnectionId}")]
    public static partial void ProjectionPathNotRegistered(
        this ILogger logger,
        string connectionId,
        string path
    );

    [LoggerMessage(
        EventId = 17,
        Level = LogLevel.Information,
        Message =
            "Received cursor moved event for brook '{BrookKey}' at position {Position} for connection {ConnectionId}")]
    public static partial void ReceivedCursorMovedEvent(
        this ILogger logger,
        string connectionId,
        string brookKey,
        long position
    );

    [LoggerMessage(
        EventId = 22,
        Level = LogLevel.Debug,
        Message =
            "Sending to SignalR group '{GroupName}' for projection {Path}/{EntityId} at version {Version} for connection {ConnectionId}")]
    public static partial void SendingToSignalRGroup(
        this ILogger logger,
        string connectionId,
        string groupName,
        string path,
        string entityId,
        long version
    );

    [LoggerMessage(
        EventId = 20,
        Level = LogLevel.Debug,
        Message =
            "Skipping older position {NewPosition} (current: {CurrentPosition}) for brook '{BrookKey}' for connection {ConnectionId}")]
    public static partial void SkippingOlderPosition(
        this ILogger logger,
        string connectionId,
        string brookKey,
        long newPosition,
        long currentPosition
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
            "Subscribed to projection {Path}/{EntityId} with subscriptionId {SubscriptionId} for connection {ConnectionId}")]
    public static partial void SubscribedToProjection(
        this ILogger logger,
        string connectionId,
        string subscriptionId,
        string path,
        string entityId
    );

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message =
            "Subscribing to projection {Path}/{EntityId} with subscriptionId {SubscriptionId} on brook {BrookName} for connection {ConnectionId}")]
    public static partial void SubscribingToProjection(
        this ILogger logger,
        string connectionId,
        string subscriptionId,
        string path,
        string entityId,
        string brookName
    );

    [LoggerMessage(
        EventId = 21,
        Level = LogLevel.Debug,
        Message = "Subscription entry not found for subscriptionId {SubscriptionId} for connection {ConnectionId}")]
    public static partial void SubscriptionEntryNotFound(
        this ILogger logger,
        string connectionId,
        string subscriptionId
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
            "Unsubscribing from projection {Path}/{EntityId} with subscriptionId {SubscriptionId} for connection {ConnectionId}")]
    public static partial void UnsubscribingFromProjection(
        this ILogger logger,
        string connectionId,
        string subscriptionId,
        string path,
        string entityId
    );
}