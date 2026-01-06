// <copyright file="UxProjectionNotificationGrainLoggerExtensions.cs" company="Gibbs-Morris">
// Proprietary and Confidential.
// All rights reserved.
// </copyright>

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
        Message = "Notification sent for projection {ProjectionKey} at position {Position} to group {GroupName}")]
    internal static partial void NotificationSent(
        this ILogger logger,
        UxProjectionKey projectionKey,
        BrookPosition position,
        string groupName
    );

    [LoggerMessage(Level = LogLevel.Debug, Message = "Notification stream completed for projection {ProjectionKey}")]
    internal static partial void NotificationStreamCompleted(
        this ILogger logger,
        UxProjectionKey projectionKey
    );

    [LoggerMessage(Level = LogLevel.Error, Message = "Error on notification stream for projection {ProjectionKey}")]
    internal static partial void NotificationStreamError(
        this ILogger logger,
        UxProjectionKey projectionKey,
        Exception ex
    );

    [LoggerMessage(Level = LogLevel.Debug, Message = "Position updated for projection {ProjectionKey}: {Position}")]
    internal static partial void PositionUpdated(
        this ILogger logger,
        UxProjectionKey projectionKey,
        BrookPosition position
    );
}