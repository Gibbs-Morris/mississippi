using System;

using Microsoft.Extensions.Logging;


namespace Cascade.Server.Components.Services;

/// <summary>
///     High-performance logging extensions for <see cref="ProjectionSubscriber{T}" />.
/// </summary>
internal static partial class ProjectionSubscriberLoggerExtensions
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Connected to SignalR hub")]
    internal static partial void ConnectedToHub(
        this ILogger logger
    );

    [LoggerMessage(Level = LogLevel.Debug, Message = "Connecting to SignalR hub")]
    internal static partial void ConnectingToHub(
        this ILogger logger
    );

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to refresh {ProjectionType}/{EntityId}")]
    internal static partial void FailedToRefresh(
        this ILogger logger,
        Exception exception,
        string projectionType,
        string entityId
    );

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to re-subscribe to {ProjectionType}/{EntityId}")]
    internal static partial void FailedToResubscribe(
        this ILogger logger,
        Exception exception,
        string projectionType,
        string entityId
    );

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to subscribe to {ProjectionType}/{EntityId}")]
    internal static partial void FailedToSubscribe(
        this ILogger logger,
        Exception exception,
        string projectionType,
        string entityId
    );

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to unsubscribe from {ProjectionType}/{EntityId}")]
    internal static partial void FailedToUnsubscribe(
        this ILogger logger,
        Exception exception,
        string projectionType,
        string entityId
    );

    [LoggerMessage(Level = LogLevel.Debug, Message = "Fetching {ProjectionType}/{EntityId} with ETag {ETag}")]
    internal static partial void FetchingProjection(
        this ILogger logger,
        string projectionType,
        string entityId,
        string eTag
    );

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message =
            "Ignoring notification for {ProjectionType}/{EntityId} version {NewVersion} (current: {CurrentVersion})")]
    internal static partial void IgnoringOldVersionNotification(
        this ILogger logger,
        string projectionType,
        string entityId,
        long newVersion,
        long currentVersion
    );

    [LoggerMessage(Level = LogLevel.Debug, Message = "Projection {ProjectionType}/{EntityId} not found")]
    internal static partial void ProjectionNotFound(
        this ILogger logger,
        string projectionType,
        string entityId
    );

    [LoggerMessage(Level = LogLevel.Debug, Message = "Projection {ProjectionType}/{EntityId} not modified")]
    internal static partial void ProjectionNotModified(
        this ILogger logger,
        string projectionType,
        string entityId
    );

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Received change notification for {ProjectionType}/{EntityId} version {NewVersion}")]
    internal static partial void ReceivedChangeNotification(
        this ILogger logger,
        string projectionType,
        string entityId,
        long newVersion
    );

    [LoggerMessage(Level = LogLevel.Debug, Message = "Received {ProjectionType}/{EntityId} version {Version}")]
    internal static partial void ReceivedProjectionVersion(
        this ILogger logger,
        string projectionType,
        string entityId,
        long? version
    );

    [LoggerMessage(Level = LogLevel.Debug, Message = "Refresh already in progress for {ProjectionType}/{EntityId}")]
    internal static partial void RefreshAlreadyInProgress(
        this ILogger logger,
        string projectionType,
        string entityId
    );

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Re-subscribed with ID {SubscriptionId} to {ProjectionType}/{EntityId}")]
    internal static partial void ResubscribedToProjection(
        this ILogger logger,
        string subscriptionId,
        string projectionType,
        string entityId
    );

    [LoggerMessage(Level = LogLevel.Information, Message = "SignalR reconnected with connection ID {ConnectionId}")]
    internal static partial void SignalRReconnected(
        this ILogger logger,
        string? connectionId
    );

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Subscribed with ID {SubscriptionId} to {ProjectionType}/{EntityId}")]
    internal static partial void SubscribedToProjection(
        this ILogger logger,
        string subscriptionId,
        string projectionType,
        string entityId
    );

    [LoggerMessage(Level = LogLevel.Debug, Message = "Subscribing to {ProjectionType} for entity {EntityId}")]
    internal static partial void SubscribingToProjection(
        this ILogger logger,
        string projectionType,
        string entityId
    );
}