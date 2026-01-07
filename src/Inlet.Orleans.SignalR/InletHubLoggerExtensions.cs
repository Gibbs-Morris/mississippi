using System;

using Microsoft.Extensions.Logging;


namespace Mississippi.Inlet.Orleans.SignalR;

/// <summary>
///     High-performance logging extensions for <see cref="InletHub" />.
/// </summary>
internal static partial class InletHubLoggerExtensions
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Client connected: {ConnectionId}")]
    public static partial void ClientConnected(
        this ILogger logger,
        string connectionId
    );

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Client disconnected: {ConnectionId}")]
    public static partial void ClientDisconnected(
        this ILogger logger,
        string connectionId,
        Exception? exception
    );

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Information,
        Message =
            "Client {ConnectionId} subscribed to projection {ProjectionType}/{EntityId} with subscriptionId {SubscriptionId}")]
    public static partial void SubscribedToProjection(
        this ILogger logger,
        string connectionId,
        string subscriptionId,
        string projectionType,
        string entityId
    );

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "Client {ConnectionId} subscribing to projection {ProjectionType}/{EntityId}")]
    public static partial void SubscribingToProjection(
        this ILogger logger,
        string connectionId,
        string projectionType,
        string entityId
    );

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Information,
        Message = "Client {ConnectionId} unsubscribed with subscriptionId {SubscriptionId}")]
    public static partial void UnsubscribedFromProjection(
        this ILogger logger,
        string connectionId,
        string subscriptionId
    );

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Debug,
        Message =
            "Client {ConnectionId} unsubscribing from projection {ProjectionType}/{EntityId} with subscriptionId {SubscriptionId}")]
    public static partial void UnsubscribingFromProjection(
        this ILogger logger,
        string connectionId,
        string subscriptionId,
        string projectionType,
        string entityId
    );
}