using System;

using Microsoft.Extensions.Logging;


namespace Mississippi.Ripples.Client;

/// <summary>
///     Logging extension methods for <see cref="SignalRRippleConnection" />.
/// </summary>
internal static partial class SignalRRippleConnectionLoggerExtensions
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "SignalR connection closed")]
    public static partial void SignalRConnectionClosed(
        ILogger logger,
        Exception exception
    );

    [LoggerMessage(Level = LogLevel.Information, Message = "SignalR connection established")]
    public static partial void SignalRConnectionEstablished(
        ILogger logger
    );

    [LoggerMessage(Level = LogLevel.Error, Message = "SignalR connection failed")]
    public static partial void SignalRConnectionFailed(
        ILogger logger,
        Exception exception
    );

    [LoggerMessage(Level = LogLevel.Information, Message = "SignalR reconnected with connection ID {ConnectionId}")]
    public static partial void SignalRReconnected(
        ILogger logger,
        string? connectionId
    );

    [LoggerMessage(Level = LogLevel.Information, Message = "SignalR attempting to reconnect")]
    public static partial void SignalRReconnecting(
        ILogger logger
    );

    [LoggerMessage(Level = LogLevel.Debug, Message = "Starting SignalR connection to {BaseUrl}")]
    public static partial void StartingSignalRConnection(
        ILogger logger,
        string baseUrl
    );

    [LoggerMessage(Level = LogLevel.Debug, Message = "Stopping SignalR connection")]
    public static partial void StoppingSignalRConnection(
        ILogger logger
    );

    [LoggerMessage(Level = LogLevel.Trace, Message = "Subscribing to projection {ProjectionType} entity {EntityId}")]
    public static partial void SubscribingToProjection(
        ILogger logger,
        string projectionType,
        string entityId
    );

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Subscription callback failed for {ProjectionType} entity {EntityId}")]
    public static partial void SubscriptionCallbackFailed(
        ILogger logger,
        string projectionType,
        string entityId,
        Exception exception
    );

    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Unsubscribing from projection {ProjectionType} entity {EntityId}")]
    public static partial void UnsubscribingFromProjection(
        ILogger logger,
        string projectionType,
        string entityId
    );
}