using System;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.UxProjections.Abstractions;


namespace Mississippi.EventSourcing.UxProjections;

/// <summary>
///     Logger extensions for <see cref="UxProjectionVersionedCacheGrain{TProjection}" />.
/// </summary>
internal static partial class UxProjectionVersionedCacheGrainLoggerExtensions
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Versioned UX projection cache grain activated with key '{PrimaryKey}' for projection " +
                  "'{ProjectionTypeName}' on brook '{BrookKey}' at version {Version}")]
    public static partial void VersionedCacheGrainActivated(
        this ILogger logger,
        string primaryKey,
        string projectionTypeName,
        BrookKey brookKey,
        BrookPosition version
    );

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Error,
        Message = "Invalid primary key format for versioned cache grain: '{PrimaryKey}'")]
    public static partial void VersionedCacheGrainInvalidPrimaryKey(
        this ILogger logger,
        string primaryKey,
        Exception exception
    );

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "Cache hit for versioned UX projection '{VersionedKey}'")]
    public static partial void VersionedCacheHit(
        this ILogger logger,
        UxProjectionVersionedKey versionedKey
    );

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Debug,
        Message = "Loaded versioned UX projection '{VersionedKey}' from snapshot")]
    public static partial void VersionedCacheLoaded(
        this ILogger logger,
        UxProjectionVersionedKey versionedKey
    );

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Debug,
        Message = "Cache miss for versioned UX projection '{VersionedKey}', fetching from snapshot")]
    public static partial void VersionedCacheMiss(
        this ILogger logger,
        UxProjectionVersionedKey versionedKey
    );
}