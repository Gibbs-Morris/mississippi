using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Snapshots.Abstractions;


namespace Mississippi.EventSourcing.Snapshots.Blob;

/// <summary>
///     High-performance logging extensions for <see cref="SnapshotStorageProvider" />.
/// </summary>
internal static partial class SnapshotStorageProviderLoggerExtensions
{
    /// <summary>
    ///     Logs when deleting all snapshots for a stream.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="streamKey">The stream key.</param>
    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Information,
        Message = "Deleting all snapshots for stream '{StreamKey}'")]
    public static partial void DeletingAllSnapshots(
        this ILogger logger,
        SnapshotStreamKey streamKey
    );

    /// <summary>
    ///     Logs when deleting a snapshot.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="snapshotKey">The snapshot key.</param>
    [LoggerMessage(EventId = 6, Level = LogLevel.Debug, Message = "Deleting snapshot '{SnapshotKey}'")]
    public static partial void DeletingSnapshot(
        this ILogger logger,
        SnapshotKey snapshotKey
    );

    /// <summary>
    ///     Logs when pruning snapshots.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="streamKey">The stream key.</param>
    /// <param name="moduliCount">The number of moduli retained.</param>
    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Information,
        Message = "Pruning snapshots for stream '{StreamKey}' with {ModuliCount} retention moduli")]
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
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Reading snapshot '{SnapshotKey}'")]
    public static partial void ReadingSnapshot(
        this ILogger logger,
        SnapshotKey snapshotKey
    );

    /// <summary>
    ///     Logs when a snapshot was found.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="snapshotKey">The snapshot key.</param>
    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "Snapshot found for key '{SnapshotKey}'")]
    public static partial void SnapshotFound(
        this ILogger logger,
        SnapshotKey snapshotKey
    );

    /// <summary>
    ///     Logs when a snapshot is detected as compressed.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="snapshotKey">The snapshot key.</param>
    [LoggerMessage(EventId = 9, Level = LogLevel.Debug, Message = "Snapshot '{SnapshotKey}' data is compressed")]
    public static partial void SnapshotIsCompressed(
        this ILogger logger,
        SnapshotKey snapshotKey
    );

    /// <summary>
    ///     Logs when a snapshot was not found.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="snapshotKey">The snapshot key.</param>
    [LoggerMessage(EventId = 3, Level = LogLevel.Debug, Message = "Snapshot not found for key '{SnapshotKey}'")]
    public static partial void SnapshotNotFound(
        this ILogger logger,
        SnapshotKey snapshotKey
    );

    /// <summary>
    ///     Logs when a snapshot was successfully written.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="snapshotKey">The snapshot key.</param>
    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Debug,
        Message = "Snapshot written successfully for key '{SnapshotKey}'")]
    public static partial void SnapshotWritten(
        this ILogger logger,
        SnapshotKey snapshotKey
    );

    /// <summary>
    ///     Logs when writing a snapshot.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="snapshotKey">The snapshot key.</param>
    [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = "Writing snapshot '{SnapshotKey}'")]
    public static partial void WritingSnapshot(
        this ILogger logger,
        SnapshotKey snapshotKey
    );
}