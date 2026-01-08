using System;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.UxProjections.Abstractions;


namespace Mississippi.EventSourcing.UxProjections.SignalR.Grains;

/// <summary>
///     High-performance logger extensions for <see cref="UxProjectionNotificationGrain" />.
/// </summary>
internal static partial class UxProjectionNotificationGrainLoggerExtensions
{
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Notification grain activated for {PrimaryKey} (projection={ProjectionType}, entity={EntityId})")]
    internal static partial void NotificationGrainActivated(
        this ILogger logger,
        string primaryKey,
        string projectionType,
        string entityId
    );

    [LoggerMessage(Level = LogLevel.Error, Message = "Notification grain received invalid primary key: {PrimaryKey}")]
    internal static partial void NotificationGrainInvalidPrimaryKey(
        this ILogger logger,
        string primaryKey,
        Exception ex
    );

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Notification sent for projection {NotificationKey} at position {Position} to group {GroupName}")]
    internal static partial void NotificationSent(
        this ILogger logger,
        UxProjectionNotificationKey notificationKey,
        BrookPosition position,
        string groupName
    );

    [LoggerMessage(Level = LogLevel.Debug, Message = "Notification stream completed for projection {NotificationKey}")]
    internal static partial void NotificationStreamCompleted(
        this ILogger logger,
        UxProjectionNotificationKey notificationKey
    );

    [LoggerMessage(Level = LogLevel.Error, Message = "Error on notification stream for projection {NotificationKey}")]
    internal static partial void NotificationStreamError(
        this ILogger logger,
        UxProjectionNotificationKey notificationKey,
        Exception ex
    );

    [LoggerMessage(Level = LogLevel.Debug, Message = "Position updated for projection {NotificationKey}: {Position}")]
    internal static partial void PositionUpdated(
        this ILogger logger,
        UxProjectionNotificationKey notificationKey,
        BrookPosition position
    );
}
