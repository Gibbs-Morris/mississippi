using System.Diagnostics;
using System.Diagnostics.Metrics;


namespace Mississippi.EventSourcing.Snapshots.Diagnostics;

/// <summary>
///     OpenTelemetry metrics for snapshot caching and persistence.
/// </summary>
internal static class SnapshotMetrics
{
    /// <summary>
    ///     The meter name for snapshot metrics.
    /// </summary>
    internal const string MeterName = "Mississippi.EventSourcing.Snapshots";

    private static readonly Meter SnapshotMeter = new(MeterName);

    private static readonly Counter<long> BaseUsed = SnapshotMeter.CreateCounter<long>(
        "snapshot.base.used",
        "bases",
        "Number of base snapshots used for delta replay.");

    // Cache metrics
    private static readonly Counter<long> CacheHits = SnapshotMeter.CreateCounter<long>(
        "snapshot.cache.hits",
        "hits",
        "Number of snapshots loaded from cache.");

    private static readonly Counter<long> CacheMisses = SnapshotMeter.CreateCounter<long>(
        "snapshot.cache.misses",
        "misses",
        "Number of snapshots requiring rebuild.");

    private static readonly Counter<long> PersistCount = SnapshotMeter.CreateCounter<long>(
        "snapshot.persist.count",
        "persists",
        "Number of snapshots persisted.");

    // Persistence metrics
    private static readonly Histogram<double> PersistDuration = SnapshotMeter.CreateHistogram<double>(
        "snapshot.persist.duration",
        "ms",
        "Time to persist snapshot.");

    // Rebuild metrics
    private static readonly Histogram<double> RebuildDuration = SnapshotMeter.CreateHistogram<double>(
        "snapshot.rebuild.duration",
        "ms",
        "Time to rebuild snapshot from events.");

    private static readonly Histogram<int> RebuildEvents = SnapshotMeter.CreateHistogram<int>(
        "snapshot.rebuild.events",
        "events",
        "Number of events replayed during rebuild.");

    private static readonly Counter<long> ReducerHashMismatches = SnapshotMeter.CreateCounter<long>(
        "snapshot.reducer.hash.mismatches",
        "mismatches",
        "Number of reducer hash mismatches requiring rebuild.");

    /// <summary>
    ///     Record use of a base snapshot for delta replay.
    /// </summary>
    /// <param name="snapshotType">The snapshot type name.</param>
    internal static void RecordBaseUsed(
        string snapshotType
    )
    {
        TagList tags = default;
        tags.Add("snapshot.type", snapshotType);
        BaseUsed.Add(1, tags);
    }

    /// <summary>
    ///     Record a cache hit.
    /// </summary>
    /// <param name="snapshotType">The snapshot type name.</param>
    internal static void RecordCacheHit(
        string snapshotType
    )
    {
        TagList tags = default;
        tags.Add("snapshot.type", snapshotType);
        CacheHits.Add(1, tags);
    }

    /// <summary>
    ///     Record a cache miss.
    /// </summary>
    /// <param name="snapshotType">The snapshot type name.</param>
    internal static void RecordCacheMiss(
        string snapshotType
    )
    {
        TagList tags = default;
        tags.Add("snapshot.type", snapshotType);
        CacheMisses.Add(1, tags);
    }

    /// <summary>
    ///     Record a snapshot persistence operation.
    /// </summary>
    /// <param name="snapshotType">The snapshot type name.</param>
    /// <param name="durationMs">The duration of the persistence in milliseconds.</param>
    /// <param name="success">Whether the persistence was successful.</param>
    internal static void RecordPersist(
        string snapshotType,
        double durationMs,
        bool success
    )
    {
        TagList tags = default;
        tags.Add("snapshot.type", snapshotType);
        tags.Add("result", success ? "success" : "failure");
        PersistCount.Add(1, tags);
        PersistDuration.Record(durationMs, tags);
    }

    /// <summary>
    ///     Record a snapshot rebuild.
    /// </summary>
    /// <param name="snapshotType">The snapshot type name.</param>
    /// <param name="durationMs">The duration of the rebuild in milliseconds.</param>
    /// <param name="eventCount">The number of events replayed.</param>
    internal static void RecordRebuild(
        string snapshotType,
        double durationMs,
        int eventCount
    )
    {
        TagList tags = default;
        tags.Add("snapshot.type", snapshotType);
        RebuildDuration.Record(durationMs, tags);
        RebuildEvents.Record(eventCount, tags);
    }

    /// <summary>
    ///     Record a reducer hash mismatch.
    /// </summary>
    /// <param name="snapshotType">The snapshot type name.</param>
    internal static void RecordReducerHashMismatch(
        string snapshotType
    )
    {
        TagList tags = default;
        tags.Add("snapshot.type", snapshotType);
        ReducerHashMismatches.Add(1, tags);
    }
}