namespace Mississippi.Ripples.Server;

using System;
using Microsoft.Extensions.Logging;

/// <summary>
/// Logger extensions for <see cref="ServerRipple{TProjection}"/>.
/// </summary>
internal static partial class ServerRippleLoggerExtensions
{
    /// <summary>
    /// Logs that a projection subscription was established.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="projectionType">The projection type name.</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="version">The initial version.</param>
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Subscribed to projection {ProjectionType} for entity {EntityId} at version {Version}")]
    public static partial void ProjectionSubscribed(
        this ILogger logger,
        string projectionType,
        string entityId,
        long? version);

    /// <summary>
    /// Logs that a projection subscription failed.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="projectionType">The projection type name.</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="exception">The exception that occurred.</param>
    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Error,
        Message = "Failed to subscribe to projection {ProjectionType} for entity {EntityId}")]
    public static partial void ProjectionSubscriptionFailed(
        this ILogger logger,
        string projectionType,
        string entityId,
        Exception exception);

    /// <summary>
    /// Logs that a projection was unsubscribed.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="projectionType">The projection type name.</param>
    /// <param name="entityId">The entity identifier.</param>
    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "Unsubscribed from projection {ProjectionType} for entity {EntityId}")]
    public static partial void ProjectionUnsubscribed(
        this ILogger logger,
        string projectionType,
        string entityId);

    /// <summary>
    /// Logs that a projection was refreshed.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="projectionType">The projection type name.</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="version">The new version.</param>
    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Debug,
        Message = "Refreshed projection {ProjectionType} for entity {EntityId} to version {Version}")]
    public static partial void ProjectionRefreshed(
        this ILogger logger,
        string projectionType,
        string entityId,
        long? version);

    /// <summary>
    /// Logs that a projection refresh failed.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="projectionType">The projection type name.</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="exception">The exception that occurred.</param>
    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Error,
        Message = "Failed to refresh projection {ProjectionType} for entity {EntityId}")]
    public static partial void ProjectionRefreshFailed(
        this ILogger logger,
        string projectionType,
        string entityId,
        Exception exception);
}
