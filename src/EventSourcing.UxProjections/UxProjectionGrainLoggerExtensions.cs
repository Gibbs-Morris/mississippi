using System;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.UxProjections.Abstractions;


namespace Mississippi.EventSourcing.UxProjections;

/// <summary>
///     Logger extensions for <see cref="UxProjectionGrainBase{TProjection, TBrook}" />.
/// </summary>
internal static partial class UxProjectionGrainLoggerExtensions
{
    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "GetAsync delegating to versioned cache for UX projection '{ProjectionKey}' at version {Version}")]
    public static partial void GetAsyncDelegatingToVersion(
        this ILogger logger,
        UxProjectionKey projectionKey,
        BrookPosition version
    );

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Debug,
        Message = "Latest version retrieved for UX projection '{ProjectionKey}': {Version}")]
    public static partial void LatestVersionRetrieved(
        this ILogger logger,
        UxProjectionKey projectionKey,
        BrookPosition version
    );

    [LoggerMessage(EventId = 5, Level = LogLevel.Debug, Message = "No events yet for UX projection '{ProjectionKey}'")]
    public static partial void NoEventsYet(
        this ILogger logger,
        UxProjectionKey projectionKey
    );

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message =
            "UX projection grain activated with key '{PrimaryKey}' for projection '{ProjectionTypeName}' on brook '{BrookKey}'")]
    public static partial void ProjectionGrainActivated(
        this ILogger logger,
        string primaryKey,
        string projectionTypeName,
        BrookKey brookKey
    );

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Error,
        Message = "Invalid primary key format for projection grain: '{PrimaryKey}'")]
    public static partial void ProjectionGrainInvalidPrimaryKey(
        this ILogger logger,
        string primaryKey,
        Exception exception
    );

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Debug,
        Message = "Versioned request completed for UX projection '{ProjectionKey}' at version {Version}")]
    public static partial void VersionedRequestCompleted(
        this ILogger logger,
        UxProjectionKey projectionKey,
        BrookPosition version
    );

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Warning,
        Message = "Versioned request for UX projection '{ProjectionKey}' has invalid version {Version}")]
    public static partial void VersionedRequestInvalidVersion(
        this ILogger logger,
        UxProjectionKey projectionKey,
        BrookPosition version
    );

    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Debug,
        Message = "Routing versioned request for UX projection '{ProjectionKey}' to version {Version}")]
    public static partial void VersionedRequestRouting(
        this ILogger logger,
        UxProjectionKey projectionKey,
        BrookPosition version
    );
}