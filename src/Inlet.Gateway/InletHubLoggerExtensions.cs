using System;

using Microsoft.Extensions.Logging;


namespace Mississippi.Inlet.Gateway;

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
            "Client {ConnectionId} subscribed to projection {Path}/{EntityId} with subscriptionId {SubscriptionId}")]
    public static partial void SubscribedToProjection(
        this ILogger logger,
        string connectionId,
        string subscriptionId,
        string path,
        string entityId
    );

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "Client {ConnectionId} subscribing to projection {Path}/{EntityId}")]
    public static partial void SubscribingToProjection(
        this ILogger logger,
        string connectionId,
        string path,
        string entityId
    );

    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Warning,
        Message =
            "Subscription authorization denied for connection {ConnectionId} path {Path}/{EntityId} user {UserId} policy {PolicyName}")]
    public static partial void SubscriptionAuthorizationDenied(
        this ILogger logger,
        string connectionId,
        string path,
        string entityId,
        string? userId,
        string? policyName
    );

    [LoggerMessage(
        EventId = 9,
        Level = LogLevel.Debug,
        Message =
            "Subscription authorization skipped for connection {ConnectionId} path {Path}/{EntityId} reason {Reason}")]
    public static partial void SubscriptionAuthorizationSkipped(
        this ILogger logger,
        string connectionId,
        string path,
        string entityId,
        string reason
    );

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Debug,
        Message =
            "Subscription authorization succeeded for connection {ConnectionId} path {Path}/{EntityId} user {UserId}")]
    public static partial void SubscriptionAuthorizationSucceeded(
        this ILogger logger,
        string connectionId,
        string path,
        string entityId,
        string? userId
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
            "Client {ConnectionId} unsubscribing from projection {Path}/{EntityId} with subscriptionId {SubscriptionId}")]
    public static partial void UnsubscribingFromProjection(
        this ILogger logger,
        string connectionId,
        string subscriptionId,
        string path,
        string entityId
    );
}