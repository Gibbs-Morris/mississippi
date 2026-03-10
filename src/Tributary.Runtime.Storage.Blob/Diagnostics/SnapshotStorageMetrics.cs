using System.Diagnostics;
using System.Diagnostics.Metrics;


namespace Mississippi.Tributary.Runtime.Storage.Blob.Diagnostics;

/// <summary>
///     OpenTelemetry metrics for Blob snapshot storage operations.
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
        "Number of Blob snapshot deletions.");

    private static readonly Counter<long> PruneCount = SnapshotStorageMeter.CreateCounter<long>(
        "blob.snapshot.prune.count",
        "prunes",
        "Number of Blob prune operations.");

    private static readonly Counter<long> ReadCount = SnapshotStorageMeter.CreateCounter<long>(
        "blob.snapshot.read.count",
        "reads",
        "Number of Blob snapshot reads.");

    private static readonly Histogram<double> ReadDuration = SnapshotStorageMeter.CreateHistogram<double>(
        "blob.snapshot.read.duration",
        "ms",
        "Blob snapshot read duration.");

    private static readonly Histogram<long> SnapshotSize = SnapshotStorageMeter.CreateHistogram<long>(
        "blob.snapshot.size",
        "bytes",
        "Blob snapshot size.");

    private static readonly Counter<long> WriteCount = SnapshotStorageMeter.CreateCounter<long>(
        "blob.snapshot.write.count",
        "writes",
        "Number of Blob snapshot writes.");

    private static readonly Histogram<double> WriteDuration = SnapshotStorageMeter.CreateHistogram<double>(
        "blob.snapshot.write.duration",
        "ms",
        "Blob snapshot write duration.");

    /// <summary>
    ///     Records a Blob snapshot deletion.
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
    ///     Records a Blob snapshot prune operation.
    /// </summary>
    /// <param name="snapshotType">The snapshot type name.</param>
    /// <param name="prunedCount">The number of pruned snapshots.</param>
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
    ///     Records a Blob snapshot read operation.
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
    ///     Records a Blob snapshot write operation.
    /// </summary>
    /// <param name="snapshotType">The snapshot type name.</param>
    /// <param name="durationMs">The duration in milliseconds.</param>
    /// <param name="success">Whether the write succeeded.</param>
    /// <param name="sizeBytes">The snapshot size in bytes.</param>
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
