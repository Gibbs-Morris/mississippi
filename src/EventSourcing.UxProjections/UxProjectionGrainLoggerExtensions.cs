using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Brooks.Abstractions;


namespace Mississippi.EventSourcing.UxProjections;

/// <summary>
///     Logger extensions for <see cref="UxProjectionGrainBase{TProjection}" />.
/// </summary>
internal static partial class UxProjectionGrainLoggerExtensions
{
    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "GetAsync delegating to versioned cache for UX projection entity '{EntityId}' at version {Version}")]
    public static partial void GetAsyncDelegatingToVersion(
        this ILogger logger,
        string entityId,
        BrookPosition version
    );

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Debug,
        Message = "Latest version retrieved for UX projection entity '{EntityId}': {Version}")]
    public static partial void LatestVersionRetrieved(
        this ILogger logger,
        string entityId,
        BrookPosition version
    );

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Debug,
        Message = "No events yet for UX projection entity '{EntityId}'")]
    public static partial void NoEventsYet(
        this ILogger logger,
        string entityId
    );

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "UX projection grain activated with entity ID '{EntityId}' on brook '{BrookName}'")]
    public static partial void ProjectionGrainActivated(
        this ILogger logger,
        string entityId,
        string brookName
    );

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Debug,
        Message = "Versioned request completed for UX projection entity '{EntityId}' at version {Version}")]
    public static partial void VersionedRequestCompleted(
        this ILogger logger,
        string entityId,
        BrookPosition version
    );

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Warning,
        Message = "Versioned request for UX projection entity '{EntityId}' has invalid version {Version}")]
    public static partial void VersionedRequestInvalidVersion(
        this ILogger logger,
        string entityId,
        BrookPosition version
    );

    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Debug,
        Message = "Routing versioned request for UX projection entity '{EntityId}' to version {Version}")]
    public static partial void VersionedRequestRouting(
        this ILogger logger,
        string entityId,
        BrookPosition version
    );
}