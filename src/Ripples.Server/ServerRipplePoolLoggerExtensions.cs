using System;

using Microsoft.Extensions.Logging;


namespace Mississippi.Ripples.Server;

/// <summary>
///     Logger extensions for <see cref="ServerRipplePool{TProjection}" />.
/// </summary>
internal static partial class ServerRipplePoolLoggerExtensions
{
    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Error,
        Message = "Pool entry fetch failed for {ProjectionType}/{EntityId}")]
    public static partial void PoolEntryFetchFailed(
        this ILogger logger,
        Exception exception,
        string projectionType,
        string entityId
    );

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Pool subscribed to {ProjectionType}: {TotalCount} requested, {NewCount} new")]
    public static partial void PoolSubscribed(
        this ILogger logger,
        string projectionType,
        int totalCount,
        int newCount
    );

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "Pool synced for {ProjectionType}: {TotalCount} total, {AddedCount} added, {RemovedCount} removed")]
    public static partial void PoolSynced(
        this ILogger logger,
        string projectionType,
        int totalCount,
        int addedCount,
        int removedCount
    );

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "Pool unsubscribed from {ProjectionType}: {RemovedCount} removed")]
    public static partial void PoolUnsubscribed(
        this ILogger logger,
        string projectionType,
        int removedCount
    );
}