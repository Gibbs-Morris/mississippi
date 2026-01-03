namespace Mississippi.Ripples.Client;

using System;

using Microsoft.Extensions.Logging;

/// <summary>
/// Logging extension methods for <see cref="ClientRipple{TProjection}"/>.
/// </summary>
internal static partial class ClientRippleLoggerExtensions
{
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Subscribed to {ProjectionType} entity {EntityId}")]
    public static partial void SubscribedToProjection(
        ILogger logger,
        string projectionType,
        string entityId);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Subscription to {ProjectionType} entity {EntityId} failed")]
    public static partial void SubscriptionFailed(
        ILogger logger,
        string projectionType,
        string entityId,
        Exception exception);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Unsubscribed from {ProjectionType} entity {EntityId}")]
    public static partial void UnsubscribedFromProjection(
        ILogger logger,
        string projectionType,
        string entityId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Refreshed {ProjectionType} entity {EntityId}")]
    public static partial void RefreshedProjection(
        ILogger logger,
        string projectionType,
        string entityId);

    [LoggerMessage(
        Level = LogLevel.Trace,
        Message = "Received version update for {ProjectionType} entity {EntityId}: v{NewVersion}")]
    public static partial void ReceivedVersionUpdate(
        ILogger logger,
        string projectionType,
        string entityId,
        long newVersion);
}
