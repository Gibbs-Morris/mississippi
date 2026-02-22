using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Snapshots.Abstractions;


namespace Mississippi.EventSourcing.Snapshots;

/// <summary>
///     High-performance logging extensions for the snapshot grain factory.
/// </summary>
internal static partial class SnapshotGrainFactoryLoggerExtensions
{
    [LoggerMessage(1, LogLevel.Debug, "Resolving {GrainType} for snapshot {SnapshotKey}")]
    public static partial void ResolvingCacheGrain(
        this ILogger logger,
        string grainType,
        SnapshotKey snapshotKey
    );

    [LoggerMessage(2, LogLevel.Debug, "Resolving {GrainType} for snapshot {SnapshotKey}")]
    public static partial void ResolvingPersisterGrain(
        this ILogger logger,
        string grainType,
        SnapshotKey snapshotKey
    );
}