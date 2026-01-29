using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Azure.Storage.Blobs.Models;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Snapshots.Abstractions;
using Mississippi.EventSourcing.Snapshots.Blob.Diagnostics;
using Mississippi.EventSourcing.Snapshots.Blob.Storage;


namespace Mississippi.EventSourcing.Snapshots.Blob;

/// <summary>
///     Azure Blob Storage implementation of <see cref="ISnapshotStorageProvider" />.
/// </summary>
internal sealed class SnapshotStorageProvider : ISnapshotStorageProvider
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SnapshotStorageProvider" /> class.
    /// </summary>
    /// <param name="blobOperations">The blob operations abstraction for Azure Blob Storage.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    public SnapshotStorageProvider(
        IBlobSnapshotOperations blobOperations,
        ILogger<SnapshotStorageProvider> logger
    )
    {
        BlobOperations = blobOperations ?? throw new ArgumentNullException(nameof(blobOperations));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string Format => "azure-blob";

    private IBlobSnapshotOperations BlobOperations { get; }

    private ILogger<SnapshotStorageProvider> Logger { get; }

    /// <inheritdoc />
    public Task DeleteAllAsync(
        SnapshotStreamKey streamKey,
        CancellationToken cancellationToken = default
    )
    {
        Logger.DeletingAllSnapshots(streamKey);
        return BlobOperations.DeleteAllAsync(streamKey, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken = default
    )
    {
        Logger.DeletingSnapshot(snapshotKey);
        await BlobOperations.DeleteAsync(snapshotKey, cancellationToken);
        SnapshotStorageMetrics.RecordDelete(snapshotKey.Stream.SnapshotStorageName);
    }

    /// <inheritdoc />
    public async Task PruneAsync(
        SnapshotStreamKey streamKey,
        IReadOnlyCollection<int> retainModuli,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(retainModuli);
        Logger.PruningSnapshots(streamKey, retainModuli.Count);
        List<string> blobNames = await BlobOperations.ListBlobNamesAsync(streamKey, cancellationToken)
            .ToListAsync(cancellationToken);
        if (blobNames.Count == 0)
        {
            return;
        }

        // Parse version numbers from blob paths
        List<(string Name, long Version)> blobVersions = new();
        string prefix = BlobPathBuilder.BuildStreamPrefix(streamKey);
        foreach (string blobName in blobNames)
        {
            // Blob path format: {storageName}/{entityId}/{reducersHash}/{version}
            string versionPart = blobName.Substring(prefix.Length);
            if (long.TryParse(versionPart, out long version))
            {
                blobVersions.Add((blobName, version));
            }
        }

        if (blobVersions.Count == 0)
        {
            return;
        }

        // Determine max version
        long maxVersion = blobVersions.Max(bv => bv.Version);

        // Delete blobs whose version doesn't match any modulus and isn't the max version
        int deletedCount = 0;
        foreach ((string Name, long Version) bv in blobVersions)
        {
            bool isMaxVersion = bv.Version == maxVersion;
            bool matchesModulus = retainModuli.Any(modulus => (bv.Version % modulus) == 0);
            if (!isMaxVersion && !matchesModulus)
            {
                await BlobOperations.DeleteAsync(new(streamKey, bv.Version), cancellationToken);
                deletedCount++;
            }
        }

        SnapshotStorageMetrics.RecordPrune(streamKey.SnapshotStorageName, deletedCount);
    }

    /// <inheritdoc />
    public async Task<SnapshotEnvelope?> ReadAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken = default
    )
    {
        Logger.ReadingSnapshot(snapshotKey);
        Stopwatch sw = Stopwatch.StartNew();
        SnapshotEnvelope? result = await BlobOperations.ReadAsync(snapshotKey, cancellationToken);
        sw.Stop();
        bool found = result is not null;
        SnapshotStorageMetrics.RecordRead(snapshotKey.Stream.SnapshotStorageName, sw.Elapsed.TotalMilliseconds, found);
        if (found)
        {
            Logger.SnapshotFound(snapshotKey);
        }
        else
        {
            Logger.SnapshotNotFound(snapshotKey);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task WriteAsync(
        SnapshotKey snapshotKey,
        SnapshotEnvelope snapshot,
        CancellationToken cancellationToken = default
    )
    {
        Logger.WritingSnapshot(snapshotKey);
        Stopwatch sw = Stopwatch.StartNew();
        try
        {
            // Check if data is compressed
            bool isCompressed = CompressionDetector.IsCompressed(snapshot.Data.AsSpan());
            if (isCompressed)
            {
                Logger.SnapshotIsCompressed(snapshotKey);
            }

            // Use Hot tier by default; could be made configurable via options if needed
            await BlobOperations.WriteAsync(snapshotKey, snapshot, AccessTier.Hot, cancellationToken);
            sw.Stop();
            SnapshotStorageMetrics.RecordWrite(
                snapshotKey.Stream.SnapshotStorageName,
                sw.Elapsed.TotalMilliseconds,
                true,
                snapshot.DataSizeBytes);
            Logger.SnapshotWritten(snapshotKey);
        }
        catch
        {
            sw.Stop();
            SnapshotStorageMetrics.RecordWrite(
                snapshotKey.Stream.SnapshotStorageName,
                sw.Elapsed.TotalMilliseconds,
                false);
            throw;
        }
    }
}