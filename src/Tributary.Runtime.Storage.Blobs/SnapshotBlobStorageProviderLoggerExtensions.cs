using System;

using Microsoft.Extensions.Logging;

using Mississippi.Tributary.Abstractions;


namespace Mississippi.Tributary.Runtime.Storage.Blobs;

/// <summary>
///     High-performance logging extensions for <see cref="SnapshotBlobStorageProvider" />.
/// </summary>
internal static partial class SnapshotBlobStorageProviderLoggerExtensions
{
    /// <summary>
    ///     Logs when all snapshots for a stream were deleted.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="streamKey">The stream key.</param>
    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "Deleted all Blob snapshots for stream '{StreamKey}'.")]
    public static partial void AllSnapshotsDeleted(
        this ILogger logger,
        SnapshotStreamKey streamKey
    );

    /// <summary>
    ///     Logs when all snapshots for a stream are being deleted.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="streamKey">The stream key.</param>
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Deleting all Blob snapshots for stream '{StreamKey}'.")]
    public static partial void DeletingAllSnapshots(
        this ILogger logger,
        SnapshotStreamKey streamKey
    );

    /// <summary>
    ///     Logs when a snapshot delete begins.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="snapshotKey">The snapshot key.</param>
    [LoggerMessage(EventId = 3, Level = LogLevel.Debug, Message = "Deleting Blob snapshot '{SnapshotKey}'.")]
    public static partial void DeletingSnapshot(
        this ILogger logger,
        SnapshotKey snapshotKey
    );

    /// <summary>
    ///     Logs when a prune operation begins.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="streamKey">The stream key.</param>
    /// <param name="moduliCount">The number of retention moduli.</param>
    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Information,
        Message = "Pruning Blob snapshots for stream '{StreamKey}' with {ModuliCount} moduli.")]
    public static partial void PruningSnapshots(
        this ILogger logger,
        SnapshotStreamKey streamKey,
        int moduliCount
    );

    /// <summary>
    ///     Logs when a snapshot read begins.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="snapshotKey">The snapshot key.</param>
    [LoggerMessage(EventId = 7, Level = LogLevel.Debug, Message = "Reading Blob snapshot '{SnapshotKey}'.")]
    public static partial void ReadingSnapshot(
        this ILogger logger,
        SnapshotKey snapshotKey
    );

    /// <summary>
    ///     Logs when a snapshot delete completes.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="snapshotKey">The snapshot key.</param>
    [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = "Deleted Blob snapshot '{SnapshotKey}'.")]
    public static partial void SnapshotDeleted(
        this ILogger logger,
        SnapshotKey snapshotKey
    );

    /// <summary>
    ///     Logs when a snapshot was found.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="snapshotKey">The snapshot key.</param>
    [LoggerMessage(EventId = 8, Level = LogLevel.Debug, Message = "Found Blob snapshot '{SnapshotKey}'.")]
    public static partial void SnapshotFound(
        this ILogger logger,
        SnapshotKey snapshotKey
    );

    /// <summary>
    ///     Logs when a snapshot was not found.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="snapshotKey">The snapshot key.</param>
    [LoggerMessage(EventId = 9, Level = LogLevel.Debug, Message = "Blob snapshot '{SnapshotKey}' was not found.")]
    public static partial void SnapshotNotFound(
        this ILogger logger,
        SnapshotKey snapshotKey
    );

    /// <summary>
    ///     Logs when a snapshot write fails.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="snapshotKey">The snapshot key.</param>
    /// <param name="exception">The exception.</param>
    [LoggerMessage(EventId = 12, Level = LogLevel.Error, Message = "Failed to write Blob snapshot '{SnapshotKey}'.")]
    public static partial void SnapshotWriteFailed(
        this ILogger logger,
        SnapshotKey snapshotKey,
        Exception exception
    );

    /// <summary>
    ///     Logs when a snapshot write completes.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="snapshotKey">The snapshot key.</param>
    [LoggerMessage(EventId = 11, Level = LogLevel.Debug, Message = "Wrote Blob snapshot '{SnapshotKey}'.")]
    public static partial void SnapshotWritten(
        this ILogger logger,
        SnapshotKey snapshotKey
    );

    /// <summary>
    ///     Logs when a prune operation completes.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="streamKey">The stream key.</param>
    /// <param name="deletedCount">The number of deleted snapshots.</param>
    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Information,
        Message = "Pruned {DeletedCount} Blob snapshots for stream '{StreamKey}'.")]
    public static partial void SnapshotsPruned(
        this ILogger logger,
        SnapshotStreamKey streamKey,
        int deletedCount
    );

    /// <summary>
    ///     Logs when a snapshot write begins.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="snapshotKey">The snapshot key.</param>
    [LoggerMessage(EventId = 10, Level = LogLevel.Debug, Message = "Writing Blob snapshot '{SnapshotKey}'.")]
    public static partial void WritingSnapshot(
        this ILogger logger,
        SnapshotKey snapshotKey
    );
}