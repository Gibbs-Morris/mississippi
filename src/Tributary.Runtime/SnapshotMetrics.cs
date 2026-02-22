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

    private const string SnapshotTypeTag = "snapshot.type";

    private static readonly Meter SnapshotMeter = new(MeterName);

    private static readonly Counter<long> ActivationCount = SnapshotMeter.CreateCounter<long>(
        "snapshot.activation.count",
        "activations",
        "Number of snapshot grain activations.");

    private static readonly Histogram<double> ActivationDuration = SnapshotMeter.CreateHistogram<double>(
        "snapshot.activation.duration",
        "ms",
        "Time to activate snapshot grain (including hydration).");

    private static readonly Counter<long> ActivationFailures = SnapshotMeter.CreateCounter<long>(
        "snapshot.activation.failures",
        "failures",
        "Number of snapshot grain activation failures.");

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
        "snapshot.event_reducer.hash.mismatches",
        "mismatches",
        "Number of event reducer hash mismatches requiring rebuild.");

    // Activation metrics

    // State size metrics
    private static readonly Histogram<long> StateSize = SnapshotMeter.CreateHistogram<long>(
        "snapshot.state.size",
        "By",
        "Serialized state size in bytes after activation.");

    /// <summary>
    ///     Record a snapshot grain activation.
    /// </summary>
    /// <param name="snapshotType">The snapshot type name.</param>
    /// <param name="durationMs">The duration of the activation in milliseconds.</param>
    /// <param name="success">Whether the activation was successful.</param>
    internal static void RecordActivation(
        string snapshotType,
        double durationMs,
        bool success
    )
    {
        TagList tags = default;
        tags.Add(SnapshotTypeTag, snapshotType);
        tags.Add("result", success ? "success" : "failure");
        ActivationCount.Add(1, tags);
        ActivationDuration.Record(durationMs, tags);
        if (!success)
        {
            ActivationFailures.Add(1, tags);
        }
    }

    /// <summary>
    ///     Record use of a base snapshot for delta replay.
    /// </summary>
    /// <param name="snapshotType">The snapshot type name.</param>
    internal static void RecordBaseUsed(
        string snapshotType
    )
    {
        TagList tags = default;
        tags.Add(SnapshotTypeTag, snapshotType);
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
        tags.Add(SnapshotTypeTag, snapshotType);
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
        tags.Add(SnapshotTypeTag, snapshotType);
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
        tags.Add(SnapshotTypeTag, snapshotType);
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
        tags.Add(SnapshotTypeTag, snapshotType);
        RebuildDuration.Record(durationMs, tags);
        RebuildEvents.Record(eventCount, tags);
    }

    /// <summary>
    ///     Record an event reducer hash mismatch.
    /// </summary>
    /// <param name="snapshotType">The snapshot type name.</param>
    internal static void RecordReducerHashMismatch(
        string snapshotType
    )
    {
        TagList tags = default;
        tags.Add(SnapshotTypeTag, snapshotType);
        ReducerHashMismatches.Add(1, tags);
    }

    /// <summary>
    ///     Record the serialized state size after activation.
    /// </summary>
    /// <param name="snapshotType">The snapshot type name.</param>
    /// <param name="sizeBytes">The serialized state size in bytes.</param>
    internal static void RecordStateSize(
        string snapshotType,
        long sizeBytes
    )
    {
        TagList tags = default;
        tags.Add(SnapshotTypeTag, snapshotType);
        StateSize.Record(sizeBytes, tags);
    }
}