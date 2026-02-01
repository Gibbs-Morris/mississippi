using System.Diagnostics;
using System.Diagnostics.Metrics;


namespace Mississippi.EventSourcing.Snapshots.Blob.Diagnostics;

/// <summary>
///     OpenTelemetry metrics for Azure Blob snapshot storage operations.
/// </summary>
internal static class BlobSnapshotStorageMetrics
{
    /// <summary>
    ///     The meter name for snapshot storage metrics.
    /// </summary>
    /// <remarks>
    ///     Shared with Cosmos provider to enable unified dashboards.
    /// </remarks>
    internal const string MeterName = "Mississippi.Storage.Snapshots";

    private const string CompressionTag = "compression";

    private const string ResultTag = "result";

    private const string SnapshotTypeTag = "snapshot.type";

    private const string TierTag = "tier";

    private static readonly Meter SnapshotStorageMeter = new(MeterName);

    private static readonly Histogram<double> CompressionRatio = SnapshotStorageMeter.CreateHistogram<double>(
        "blob.snapshot.compression.ratio",
        "ratio",
        "Compression ratio (uncompressed/compressed).");

    // Delete metrics
    private static readonly Counter<long> DeleteCount = SnapshotStorageMeter.CreateCounter<long>(
        "blob.snapshot.delete.count",
        "deletes",
        "Number of snapshot deletions from blob storage.");

    // Prune metrics
    private static readonly Counter<long> PruneCount = SnapshotStorageMeter.CreateCounter<long>(
        "blob.snapshot.prune.count",
        "prunes",
        "Number of prune operations on blob storage.");

    // Read metrics
    private static readonly Counter<long> ReadCount = SnapshotStorageMeter.CreateCounter<long>(
        "blob.snapshot.read.count",
        "reads",
        "Number of snapshot reads from blob storage.");

    private static readonly Histogram<double> ReadDuration = SnapshotStorageMeter.CreateHistogram<double>(
        "blob.snapshot.read.duration",
        "ms",
        "Snapshot read duration from blob storage.");

    // Size metrics
    private static readonly Histogram<long> SnapshotSize = SnapshotStorageMeter.CreateHistogram<long>(
        "blob.snapshot.size",
        "bytes",
        "Snapshot size in bytes.");

    // Compression ratio metrics

    // Write metrics
    private static readonly Counter<long> WriteCount = SnapshotStorageMeter.CreateCounter<long>(
        "blob.snapshot.write.count",
        "writes",
        "Number of snapshot writes to blob storage.");

    private static readonly Histogram<double> WriteDuration = SnapshotStorageMeter.CreateHistogram<double>(
        "blob.snapshot.write.duration",
        "ms",
        "Snapshot write duration to blob storage.");

    /// <summary>
    ///     Record a compression ratio measurement.
    /// </summary>
    /// <param name="snapshotType">The snapshot type name.</param>
    /// <param name="compression">The compression type used.</param>
    /// <param name="originalSizeBytes">The original uncompressed size.</param>
    /// <param name="compressedSizeBytes">The compressed size.</param>
    internal static void RecordCompressionRatio(
        string snapshotType,
        string compression,
        long originalSizeBytes,
        long compressedSizeBytes
    )
    {
        if ((originalSizeBytes <= 0) || (compressedSizeBytes <= 0) || string.IsNullOrEmpty(compression))
        {
            return;
        }

        TagList sizeTags = default;
        sizeTags.Add(SnapshotTypeTag, snapshotType);
        sizeTags.Add(CompressionTag, compression);
        double ratio = (double)originalSizeBytes / compressedSizeBytes;
        CompressionRatio.Record(ratio, sizeTags);
    }

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
        tags.Add(ResultTag, found ? "found" : "not_found");
        ReadCount.Add(1, tags);
        ReadDuration.Record(durationMs, tags);
    }

    /// <summary>
    ///     Record a snapshot write operation.
    /// </summary>
    /// <param name="snapshotType">The snapshot type name.</param>
    /// <param name="durationMs">The duration in milliseconds.</param>
    /// <param name="success">Whether the write was successful.</param>
    /// <param name="compression">The compression type used.</param>
    /// <param name="tier">The access tier used.</param>
    /// <param name="originalSizeBytes">The original uncompressed size.</param>
    /// <param name="compressedSizeBytes">The compressed size.</param>
    internal static void RecordWrite(
        string snapshotType,
        double durationMs,
        bool success,
        string compression = "",
        string tier = "",
        long originalSizeBytes = 0,
        long compressedSizeBytes = 0
    )
    {
        TagList tags = default;
        tags.Add(SnapshotTypeTag, snapshotType);
        tags.Add(ResultTag, success ? "success" : "failure");
        tags.Add(CompressionTag, compression);
        tags.Add(TierTag, tier);
        WriteCount.Add(1, tags);
        WriteDuration.Record(durationMs, tags);
        if (originalSizeBytes > 0)
        {
            TagList sizeTags = default;
            sizeTags.Add(SnapshotTypeTag, snapshotType);
            sizeTags.Add(CompressionTag, compression);
            SnapshotSize.Record(originalSizeBytes, sizeTags);
            if ((compressedSizeBytes > 0) && !string.IsNullOrEmpty(compression))
            {
                double ratio = (double)originalSizeBytes / compressedSizeBytes;
                CompressionRatio.Record(ratio, sizeTags);
            }
        }
    }
}