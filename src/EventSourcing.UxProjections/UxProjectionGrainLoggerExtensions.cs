using System;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.UxProjections.Abstractions;


namespace Mississippi.EventSourcing.UxProjections;

/// <summary>
///     Logger extensions for <see cref="UxProjectionGrain{TProjection, TBrook}" />.
/// </summary>
internal static partial class UxProjectionGrainLoggerExtensions
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "UX projection grain activated with key '{PrimaryKey}' for projection '{ProjectionTypeName}' on brook '{BrookKey}'")]
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
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "Cache hit for UX projection '{ProjectionKey}' at version {CachedVersion}")]
    public static partial void CacheHit(
        this ILogger logger,
        UxProjectionKey projectionKey,
        BrookPosition cachedVersion
    );

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Debug,
        Message = "Cache miss for UX projection '{ProjectionKey}': cached version {CachedVersion}, current version {CurrentVersion}")]
    public static partial void CacheMiss(
        this ILogger logger,
        UxProjectionKey projectionKey,
        BrookPosition cachedVersion,
        BrookPosition currentVersion
    );

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Debug,
        Message = "Cache updated for UX projection '{ProjectionKey}' to version {NewVersion}")]
    public static partial void CacheUpdated(
        this ILogger logger,
        UxProjectionKey projectionKey,
        BrookPosition newVersion
    );

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Debug,
        Message = "No events yet for UX projection '{ProjectionKey}'")]
    public static partial void NoEventsYet(
        this ILogger logger,
        UxProjectionKey projectionKey
    );
}
