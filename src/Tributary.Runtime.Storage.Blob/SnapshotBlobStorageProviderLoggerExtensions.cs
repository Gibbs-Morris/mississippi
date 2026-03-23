using System;

using Microsoft.Extensions.Logging;

using Mississippi.Tributary.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Blob.Storage;


namespace Mississippi.Tributary.Runtime.Storage.Blob;

/// <summary>
///     Logger extensions for the Blob snapshot storage provider.
/// </summary>
internal static partial class SnapshotBlobStorageProviderLoggerExtensions
{
    /// <summary>
    ///     Logs when delete-all has completed for a stream.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="streamKey">The stream key.</param>
    [LoggerMessage(
        EventId = 2401,
        Level = LogLevel.Information,
        Message = "Deleted all Blob snapshots for stream '{streamKey}'.")]
    public static partial void DeletedAllSnapshots(
        this ILogger logger,
        SnapshotStreamKey streamKey
    );

    /// <summary>
    ///     Logs when a snapshot delete has completed.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="snapshotKey">The snapshot key.</param>
    [LoggerMessage(EventId = 2403, Level = LogLevel.Debug, Message = "Deleted Blob snapshot '{snapshotKey}'.")]
    public static partial void DeletedSnapshot(
        this ILogger logger,
        SnapshotKey snapshotKey
    );

    /// <summary>
    ///     Logs when deleting all snapshots for a stream.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="streamKey">The stream key.</param>
    [LoggerMessage(
        EventId = 2400,
        Level = LogLevel.Information,
        Message = "Deleting all Blob snapshots for stream '{streamKey}'.")]
    public static partial void DeletingAllSnapshots(
        this ILogger logger,
        SnapshotStreamKey streamKey
    );

    /// <summary>
    ///     Logs when deleting a snapshot.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="snapshotKey">The snapshot key.</param>
    [LoggerMessage(EventId = 2402, Level = LogLevel.Debug, Message = "Deleting Blob snapshot '{snapshotKey}'.")]
    public static partial void DeletingSnapshot(
        this ILogger logger,
        SnapshotKey snapshotKey
    );

    /// <summary>
    ///     Logs when prune has completed.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="streamKey">The stream key.</param>
    [LoggerMessage(
        EventId = 2405,
        Level = LogLevel.Information,
        Message = "Pruned Blob snapshots for stream '{streamKey}'.")]
    public static partial void PrunedSnapshots(
        this ILogger logger,
        SnapshotStreamKey streamKey
    );

    /// <summary>
    ///     Logs when pruning snapshots.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="streamKey">The stream key.</param>
    /// <param name="moduliCount">The number of retain moduli.</param>
    [LoggerMessage(
        EventId = 2404,
        Level = LogLevel.Information,
        Message = "Pruning Blob snapshots for stream '{streamKey}' with {moduliCount} retain moduli.")]
    public static partial void PruningSnapshots(
        this ILogger logger,
        SnapshotStreamKey streamKey,
        int moduliCount
    );

    /// <summary>
    ///     Logs when reading a snapshot.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="snapshotKey">The snapshot key.</param>
    [LoggerMessage(EventId = 2406, Level = LogLevel.Debug, Message = "Reading Blob snapshot '{snapshotKey}'.")]
    public static partial void ReadingSnapshot(
        this ILogger logger,
        SnapshotKey snapshotKey
    );

    /// <summary>
    ///     Logs when a snapshot was found.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="snapshotKey">The snapshot key.</param>
    [LoggerMessage(EventId = 2407, Level = LogLevel.Debug, Message = "Blob snapshot found for key '{snapshotKey}'.")]
    public static partial void SnapshotFound(
        this ILogger logger,
        SnapshotKey snapshotKey
    );

    /// <summary>
    ///     Logs when a snapshot was not found.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="snapshotKey">The snapshot key.</param>
    [LoggerMessage(
        EventId = 2408,
        Level = LogLevel.Debug,
        Message = "Blob snapshot not found for key '{snapshotKey}'.")]
    public static partial void SnapshotNotFound(
        this ILogger logger,
        SnapshotKey snapshotKey
    );

    /// <summary>
    ///     Logs when a write fails because the target snapshot version already exists.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="snapshotKey">The conflicting snapshot key.</param>
    /// <param name="exception">The underlying conflict exception.</param>
    [LoggerMessage(
        EventId = 2411,
        Level = LogLevel.Warning,
        Message = "Blob snapshot write conflict for key '{snapshotKey}'.")]
    public static partial void SnapshotWriteConflict(
        this ILogger logger,
        SnapshotKey snapshotKey,
        Exception exception
    );

    /// <summary>
    ///     Logs when a snapshot write has completed.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="snapshotKey">The snapshot key.</param>
    [LoggerMessage(EventId = 2410, Level = LogLevel.Debug, Message = "Blob snapshot written for key '{snapshotKey}'.")]
    public static partial void SnapshotWritten(
        this ILogger logger,
        SnapshotKey snapshotKey
    );

    /// <summary>
    ///     Logs when a stored Blob exists but cannot be read as a valid snapshot frame.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="snapshotKey">The snapshot key.</param>
    /// <param name="reason">The unreadable frame reason.</param>
    /// <param name="exception">The underlying unreadable-frame exception.</param>
    [LoggerMessage(
        EventId = 2412,
        Level = LogLevel.Error,
        Message = "Stored Blob snapshot '{snapshotKey}' is unreadable with reason '{reason}'.")]
    public static partial void UnreadableSnapshotBlob(
        this ILogger logger,
        SnapshotKey snapshotKey,
        SnapshotBlobUnreadableFrameReason reason,
        Exception exception
    );

    /// <summary>
    ///     Logs when writing a snapshot.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="snapshotKey">The snapshot key.</param>
    [LoggerMessage(EventId = 2409, Level = LogLevel.Debug, Message = "Writing Blob snapshot '{snapshotKey}'.")]
    public static partial void WritingSnapshot(
        this ILogger logger,
        SnapshotKey snapshotKey
    );
}