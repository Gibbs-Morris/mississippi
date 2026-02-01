using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Azure.Storage.Blobs.Models;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.EventSourcing.Snapshots.Abstractions;
using Mississippi.EventSourcing.Snapshots.Blob.Diagnostics;


namespace Mississippi.EventSourcing.Snapshots.Blob;

/// <summary>
///     Azure Blob Storage implementation of <see cref="ISnapshotStorageProvider" />.
/// </summary>
internal sealed class BlobSnapshotStorageProvider : ISnapshotStorageProvider
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BlobSnapshotStorageProvider" /> class.
    /// </summary>
    /// <param name="repository">The blob repository handling snapshot persistence.</param>
    /// <param name="options">The storage options.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    public BlobSnapshotStorageProvider(
        IBlobSnapshotRepository repository,
        IOptions<BlobSnapshotStorageOptions> options,
        ILogger<BlobSnapshotStorageProvider> logger
    )
    {
        Repository = repository ?? throw new ArgumentNullException(nameof(repository));
        Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        if (Options.DefaultAccessTier == AccessTier.Archive)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                Options.DefaultAccessTier,
                "Archive access tier is not supported for snapshot storage.");
        }

        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string Format => "azure-blob";

    private ILogger<BlobSnapshotStorageProvider> Logger { get; }

    private BlobSnapshotStorageOptions Options { get; }

    private IBlobSnapshotRepository Repository { get; }

    private static string GetCompressionName(
        SnapshotCompression compression
    ) =>
        compression switch
        {
            SnapshotCompression.GZip => "gzip",
            SnapshotCompression.Brotli => "brotli",
            var _ => "none",
        };

    /// <inheritdoc />
    public Task DeleteAllAsync(
        SnapshotStreamKey streamKey,
        CancellationToken cancellationToken = default
    )
    {
        Logger.DeletingAllSnapshots(streamKey);
        return Repository.DeleteAllAsync(streamKey, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken = default
    )
    {
        Logger.DeletingSnapshot(snapshotKey);
        await Repository.DeleteAsync(snapshotKey, cancellationToken).ConfigureAwait(false);
        BlobSnapshotStorageMetrics.RecordDelete(snapshotKey.Stream.SnapshotStorageName);
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
        await Repository.PruneAsync(streamKey, retainModuli, cancellationToken).ConfigureAwait(false);
        BlobSnapshotStorageMetrics.RecordPrune(streamKey.SnapshotStorageName, retainModuli.Count);
    }

    /// <inheritdoc />
    public async Task<SnapshotEnvelope?> ReadAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken = default
    )
    {
        Logger.ReadingSnapshot(snapshotKey);
        Stopwatch sw = Stopwatch.StartNew();
        SnapshotEnvelope? result = await Repository.ReadAsync(snapshotKey, cancellationToken).ConfigureAwait(false);
        sw.Stop();
        bool found = result is not null;
        BlobSnapshotStorageMetrics.RecordRead(
            snapshotKey.Stream.SnapshotStorageName,
            sw.Elapsed.TotalMilliseconds,
            found);
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
        ArgumentNullException.ThrowIfNull(snapshot);
        Logger.WritingSnapshot(snapshotKey);
        Stopwatch sw = Stopwatch.StartNew();
        try
        {
            await Repository.WriteAsync(snapshotKey, snapshot, cancellationToken).ConfigureAwait(false);
            sw.Stop();
            BlobSnapshotStorageMetrics.RecordWrite(
                snapshotKey.Stream.SnapshotStorageName,
                sw.Elapsed.TotalMilliseconds,
                true,
                GetCompressionName(Options.WriteCompression),
                Options.DefaultAccessTier.ToString(),
                snapshot.DataSizeBytes);
            Logger.SnapshotWritten(snapshotKey);
        }
        catch
        {
            sw.Stop();
            BlobSnapshotStorageMetrics.RecordWrite(
                snapshotKey.Stream.SnapshotStorageName,
                sw.Elapsed.TotalMilliseconds,
                false);
            throw;
        }
    }
}