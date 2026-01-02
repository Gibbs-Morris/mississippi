using Microsoft.Extensions.Logging;


namespace Mississippi.EventSourcing.UxProjections.Api;

/// <summary>
///     High-performance logging extensions for UX projection controller operations.
/// </summary>
internal static partial class UxProjectionControllerLoggerExtensions
{
    /// <summary>
    ///     Logs a request for the latest projection state.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Getting latest projection for entity '{EntityId}' of type {ProjectionType}")]
    public static partial void GettingLatestProjection(
        this ILogger logger,
        string entityId,
        string projectionType
    );

    /// <summary>
    ///     Logs a request for the latest version of a projection.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Getting latest version for entity '{EntityId}' of type {ProjectionType}")]
    public static partial void GettingLatestVersion(
        this ILogger logger,
        string entityId,
        string projectionType
    );

    /// <summary>
    ///     Logs a request for a projection at a specific version.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Getting projection at version {Version} for entity '{EntityId}' of type {ProjectionType}")]
    public static partial void GettingProjectionAtVersion(
        this ILogger logger,
        string entityId,
        long version,
        string projectionType
    );

    /// <summary>
    ///     Logs when the latest version is retrieved.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Latest version is {Version} for entity '{EntityId}' of type {ProjectionType}")]
    public static partial void LatestVersionRetrieved(
        this ILogger logger,
        string entityId,
        long version,
        string projectionType
    );

    /// <summary>
    ///     Logs when no version exists for an entity.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "No version found for entity '{EntityId}' of type {ProjectionType}")]
    public static partial void NoVersionFound(
        this ILogger logger,
        string entityId,
        string projectionType
    );

    /// <summary>
    ///     Logs when a projection at a specific version is not found.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Projection at version {Version} not found for entity '{EntityId}' of type {ProjectionType}")]
    public static partial void ProjectionAtVersionNotFound(
        this ILogger logger,
        string entityId,
        long version,
        string projectionType
    );

    /// <summary>
    ///     Logs a successful projection retrieval at a specific version.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Retrieved projection at version {Version} for entity '{EntityId}' of type {ProjectionType}")]
    public static partial void ProjectionAtVersionRetrieved(
        this ILogger logger,
        string entityId,
        long version,
        string projectionType
    );

    /// <summary>
    ///     Logs when a projection is not found.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Projection not found for entity '{EntityId}' of type {ProjectionType}")]
    public static partial void ProjectionNotFound(
        this ILogger logger,
        string entityId,
        string projectionType
    );

    /// <summary>
    ///     Logs when returning 304 Not Modified due to matching ETag.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Returning 304 Not Modified for entity '{EntityId}' at version {Version} of type {ProjectionType}")]
    public static partial void ProjectionNotModified(
        this ILogger logger,
        string entityId,
        long version,
        string projectionType
    );

    /// <summary>
    ///     Logs a successful projection retrieval.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Retrieved projection for entity '{EntityId}' of type {ProjectionType}")]
    public static partial void ProjectionRetrieved(
        this ILogger logger,
        string entityId,
        string projectionType
    );
}