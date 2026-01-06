using System;

using Microsoft.Extensions.Logging;


namespace Mississippi.Ripples.InProcess;

/// <summary>
///     Logging extension methods for <see cref="InProcessProjectionUpdateNotifier" />.
/// </summary>
internal static partial class InProcessProjectionUpdateNotifierLoggerExtensions
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Callback failed for {ProjectionType} entity {EntityId}")]
    public static partial void CallbackFailed(
        ILogger logger,
        string projectionType,
        string entityId,
        Exception exception
    );

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "Notifying {SubscriberCount} subscribers for {ProjectionType} entity {EntityId} at version {NewVersion}")]
    public static partial void NotifyingSubscribers(
        ILogger logger,
        int subscriberCount,
        string projectionType,
        string entityId,
        long newVersion
    );

    [LoggerMessage(Level = LogLevel.Debug, Message = "Subscription created for {ProjectionType} entity {EntityId}")]
    public static partial void SubscriptionCreated(
        ILogger logger,
        string projectionType,
        string entityId
    );

    [LoggerMessage(Level = LogLevel.Debug, Message = "Subscription removed for {ProjectionType} entity {EntityId}")]
    public static partial void SubscriptionRemoved(
        ILogger logger,
        string projectionType,
        string entityId
    );
}