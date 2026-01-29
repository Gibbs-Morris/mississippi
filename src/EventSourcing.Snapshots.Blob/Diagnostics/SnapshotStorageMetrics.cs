using System.Diagnostics;
using System.Diagnostics.Metrics;


namespace Mississippi.EventSourcing.Snapshots.Blob.Diagnostics;

/// <summary>
///     OpenTelemetry metrics for Azure Blob snapshot storage operations.
/// </summary>
internal static class SnapshotStorageMetrics
{
    /// <summary>
    ///     The meter name for snapshot storage metrics.
    /// </summary>
    internal const string MeterName = "Mississippi.Storage.Snapshots";

    private const string SnapshotTypeTag = "snapshot.type";

    private static readonly Meter SnapshotStorageMeter = new(MeterName);

    private static readonly Counter<long> DeleteCount = SnapshotStorageMeter.CreateCounter<long>(
        "blob.snapshot.delete.count",
        "deletes",
        "Number of snapshot deletions.");

    private static readonly Counter<long> PruneCount = SnapshotStorageMeter.CreateCounter<long>(
        "blob.snapshot.prune.count",
        "prunes",
        "Number of prune operations.");

    // Read metrics
    private static readonly Counter<long> ReadCount = SnapshotStorageMeter.CreateCounter<long>(
        "blob.snapshot.read.count",
        "reads",
        "Number of snapshot reads.");

    private static readonly Histogram<double> ReadDuration = SnapshotStorageMeter.CreateHistogram<double>(
        "blob.snapshot.read.duration",
        "ms",
        "Snapshot read duration.");

    // Size metrics
    private static readonly Histogram<long> SnapshotSize = SnapshotStorageMeter.CreateHistogram<long>(
        "blob.snapshot.size",
        "bytes",
        "Snapshot size in bytes.");

    // Delete/Prune metrics

    // Write metrics
    private static readonly Counter<long> WriteCount = SnapshotStorageMeter.CreateCounter<long>(
        "blob.snapshot.write.count",
        "writes",
        "Number of snapshot writes.");

    private static readonly Histogram<double> WriteDuration = SnapshotStorageMeter.CreateHistogram<double>(
        "blob.snapshot.write.duration",
        "ms",
        "Snapshot write duration.");

    /// <summary>
    ///     Record a snapshot deletion.
    /// </summary>
    /// <param name="snapshotType">The snapshot type name.</param>
    internal static void RecordDelete(
        string snapshotType
    )
    {
        TagList tags = default;
        tags.Add(SnapshotTypeTag, snapshotType);
        DeleteCount.Add(1, tags);
    }

    /// <summary>
    ///     Record a prune operation.
    /// </summary>
    /// <param name="snapshotType">The snapshot type name.</param>
    /// <param name="prunedCount">The number of snapshots pruned.</param>
    internal static void RecordPrune(
        string snapshotType,
        int prunedCount
    )
    {
        if (prunedCount <= 0)
        {
            return;
        }

        TagList tags = default;
        tags.Add(SnapshotTypeTag, snapshotType);
        PruneCount.Add(prunedCount, tags);
    }

    /// <summary>
    ///     Record a snapshot read operation.
    /// </summary>
    /// <param name="snapshotType">The snapshot type name.</param>
    /// <param name="durationMs">The duration in milliseconds.</param>
    /// <param name="found">Whether the snapshot was found.</param>
    internal static void RecordRead(
        string snapshotType,
        double durationMs,
        bool found
    )
    {
        TagList tags = default;
        tags.Add(SnapshotTypeTag, snapshotType);
        tags.Add("result", found ? "found" : "not_found");
        ReadCount.Add(1, tags);
        ReadDuration.Record(durationMs, tags);
    }

    /// <summary>
    ///     Record a snapshot write operation.
    /// </summary>
    /// <param name="snapshotType">The snapshot type name.</param>
    /// <param name="durationMs">The duration in milliseconds.</param>
    /// <param name="success">Whether the write was successful.</param>
    /// <param name="sizeBytes">The size of the snapshot in bytes.</param>
    internal static void RecordWrite(
        string snapshotType,
        double durationMs,
        bool success,
        long sizeBytes = 0
    )
    {
        TagList tags = default;
        tags.Add(SnapshotTypeTag, snapshotType);
        tags.Add("result", success ? "success" : "failure");
        WriteCount.Add(1, tags);
        WriteDuration.Record(durationMs, tags);
        if (sizeBytes > 0)
        {
            TagList sizeTags = default;
            sizeTags.Add(SnapshotTypeTag, snapshotType);
            SnapshotSize.Record(sizeBytes, sizeTags);
        }
    }
}