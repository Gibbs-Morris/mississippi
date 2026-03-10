using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.Tributary.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Blob.Diagnostics;


namespace Mississippi.Tributary.Runtime.Storage.Blob;

/// <summary>
///     Azure Blob Storage implementation of <see cref="ISnapshotStorageProvider" />.
/// </summary>
internal sealed class SnapshotStorageProvider : ISnapshotStorageProvider
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SnapshotStorageProvider" /> class.
    /// </summary>
    /// <param name="repository">The Blob repository handling snapshot persistence.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    public SnapshotStorageProvider(
        ISnapshotBlobRepository repository,
        ILogger<SnapshotStorageProvider> logger
    )
    {
        Repository = repository ?? throw new ArgumentNullException(nameof(repository));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string Format => "azure-blob-storage";

    private ILogger<SnapshotStorageProvider> Logger { get; }

    private ISnapshotBlobRepository Repository { get; }

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
        await Repository.PruneAsync(streamKey, retainModuli, cancellationToken).ConfigureAwait(false);
        SnapshotStorageMetrics.RecordPrune(streamKey.SnapshotStorageName, retainModuli.Count);
    }

    /// <inheritdoc />
    public async Task<SnapshotEnvelope?> ReadAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken = default
    )
    {
        Logger.ReadingSnapshot(snapshotKey);
        Stopwatch stopwatch = Stopwatch.StartNew();
        SnapshotEnvelope? snapshot = await Repository.ReadAsync(snapshotKey, cancellationToken).ConfigureAwait(false);
        stopwatch.Stop();
        bool found = snapshot is not null;
        SnapshotStorageMetrics.RecordRead(snapshotKey.Stream.SnapshotStorageName, stopwatch.Elapsed.TotalMilliseconds, found);
        if (found)
        {
            Logger.SnapshotFound(snapshotKey);
        }
        else
        {
            Logger.SnapshotNotFound(snapshotKey);
        }

        return snapshot;
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
        Stopwatch stopwatch = Stopwatch.StartNew();
        try
        {
            await Repository.WriteAsync(snapshotKey, snapshot, cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();
            SnapshotStorageMetrics.RecordWrite(
                snapshotKey.Stream.SnapshotStorageName,
                stopwatch.Elapsed.TotalMilliseconds,
                true,
                snapshot.DataSizeBytes);
            Logger.SnapshotWritten(snapshotKey);
        }
        catch
        {
            stopwatch.Stop();
            SnapshotStorageMetrics.RecordWrite(
                snapshotKey.Stream.SnapshotStorageName,
                stopwatch.Elapsed.TotalMilliseconds,
                false,
                snapshot.DataSizeBytes);
            throw;
        }
    }
}
