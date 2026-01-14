using Microsoft.Extensions.Logging;


namespace Mississippi.EventSourcing.Snapshots;

/// <summary>
///     High-performance logging extensions for snapshot cache grains.
/// </summary>
internal static partial class SnapshotCacheGrainLoggerExtensions
{
    [LoggerMessage(8, LogLevel.Debug, "Snapshot cache grain activated for key {SnapshotKey}")]
    public static partial void Activated(
        this ILogger logger,
        string snapshotKey
    );

    [LoggerMessage(1, LogLevel.Debug, "Activating snapshot cache grain for key {SnapshotKey}")]
    public static partial void Activating(
        this ILogger logger,
        string snapshotKey
    );

    [LoggerMessage(10, LogLevel.Debug, "No base snapshot available for key {SnapshotKey}, rebuilding from beginning")]
    public static partial void NoBaseSnapshotAvailable(
        this ILogger logger,
        string snapshotKey
    );

    [LoggerMessage(4, LogLevel.Debug, "No snapshot found in storage for key {SnapshotKey}, rebuilding from stream")]
    public static partial void NoSnapshotInStorage(
        this ILogger logger,
        string snapshotKey
    );

    [LoggerMessage(5, LogLevel.Debug, "Rebuilding state from brook {BrookKey} for snapshot {SnapshotKey}")]
    public static partial void RebuildingFromStream(
        this ILogger logger,
        string brookKey,
        string snapshotKey
    );

    [LoggerMessage(
        3,
        LogLevel.Information,
        "Snapshot event reducer hash mismatch for key {SnapshotKey} (stored: {StoredHash}, current: {CurrentHash}), rebuilding from stream")]
    public static partial void ReducerHashMismatch(
        this ILogger logger,
        string snapshotKey,
        string storedHash,
        string currentHash
    );

    [LoggerMessage(7, LogLevel.Debug, "Requesting background persistence for snapshot {SnapshotKey}")]
    public static partial void RequestingPersistence(
        this ILogger logger,
        string snapshotKey
    );

    [LoggerMessage(2, LogLevel.Debug, "Loaded snapshot from storage for key {SnapshotKey}")]
    public static partial void SnapshotLoadedFromStorage(
        this ILogger logger,
        string snapshotKey
    );

    [LoggerMessage(6, LogLevel.Debug, "State rebuilt from {EventCount} events for snapshot {SnapshotKey}")]
    public static partial void StateRebuilt(
        this ILogger logger,
        long eventCount,
        string snapshotKey
    );

    [LoggerMessage(
        9,
        LogLevel.Debug,
        "Using base snapshot at version {BaseVersion} for target {TargetVersion}, replaying {DeltaEvents} events")]
    public static partial void UsingBaseSnapshot(
        this ILogger logger,
        long baseVersion,
        long targetVersion,
        long deltaEvents
    );
}