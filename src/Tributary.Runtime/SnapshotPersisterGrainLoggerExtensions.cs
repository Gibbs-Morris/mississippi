using System;

using Microsoft.Extensions.Logging;


namespace Mississippi.EventSourcing.Snapshots;

/// <summary>
///     High-performance logging extensions for snapshot persister grains.
/// </summary>
internal static partial class SnapshotPersisterGrainLoggerExtensions
{
    [LoggerMessage(3, LogLevel.Warning, "Failed to persist snapshot for key {SnapshotKey}: {ErrorMessage}")]
    public static partial void PersistenceFailed(
        this ILogger logger,
        Exception exception,
        string snapshotKey,
        string errorMessage
    );

    [LoggerMessage(1, LogLevel.Debug, "Persisting snapshot for key {SnapshotKey}")]
    public static partial void PersistingSnapshot(
        this ILogger logger,
        string snapshotKey
    );

    [LoggerMessage(2, LogLevel.Debug, "Snapshot persisted successfully for key {SnapshotKey}")]
    public static partial void SnapshotPersisted(
        this ILogger logger,
        string snapshotKey
    );
}