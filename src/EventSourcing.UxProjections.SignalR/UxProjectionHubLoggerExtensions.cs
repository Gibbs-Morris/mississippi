using System;

using Microsoft.Extensions.Logging;


namespace Mississippi.EventSourcing.UxProjections.SignalR;

/// <summary>
///     Logger extensions for <see cref="UxProjectionHub" />.
/// </summary>
internal static partial class UxProjectionHubLoggerExtensions
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Client connected: {ConnectionId}")]
    public static partial void ClientConnected(
        this ILogger logger,
        string connectionId
    );

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "Client disconnected: {ConnectionId}")]
    public static partial void ClientDisconnected(
        this ILogger logger,
        string connectionId,
        Exception? exception
    );

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "Client '{ConnectionId}' subscribing to projection '{ProjectionType}' for entity '{EntityId}'")]
    public static partial void SubscribingToProjection(
        this ILogger logger,
        string connectionId,
        string projectionType,
        string entityId
    );

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Information,
        Message = "Client '{ConnectionId}' subscribed with ID '{SubscriptionId}' to projection '{ProjectionType}' for entity '{EntityId}'")]
    public static partial void SubscribedToProjection(
        this ILogger logger,
        string connectionId,
        string subscriptionId,
        string projectionType,
        string entityId
    );

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Debug,
        Message = "Client '{ConnectionId}' unsubscribing '{SubscriptionId}' from projection '{ProjectionType}' for entity '{EntityId}'")]
    public static partial void UnsubscribingFromProjection(
        this ILogger logger,
        string connectionId,
        string subscriptionId,
        string projectionType,
        string entityId
    );

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Information,
        Message = "Client '{ConnectionId}' unsubscribed '{SubscriptionId}'")]
    public static partial void UnsubscribedFromProjection(
        this ILogger logger,
        string connectionId,
        string subscriptionId
    );
}
