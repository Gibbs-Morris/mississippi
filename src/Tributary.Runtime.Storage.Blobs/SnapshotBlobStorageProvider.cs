using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.Tributary.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Blobs.Diagnostics;


namespace Mississippi.Tributary.Runtime.Storage.Blobs;

/// <summary>
///     Azure Blob Storage implementation of <see cref="ISnapshotStorageProvider" />.
/// </summary>
internal sealed class SnapshotBlobStorageProvider : ISnapshotStorageProvider
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SnapshotBlobStorageProvider" /> class.
    /// </summary>
    /// <param name="repository">The repository handling Blob snapshot persistence.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    public SnapshotBlobStorageProvider(
        ISnapshotBlobRepository repository,
        ILogger<SnapshotBlobStorageProvider> logger
    )
    {
        Repository = repository ?? throw new ArgumentNullException(nameof(repository));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string Format => "azure-blob";

    private ILogger<SnapshotBlobStorageProvider> Logger { get; }

    private ISnapshotBlobRepository Repository { get; }

    /// <inheritdoc />
    public async Task DeleteAllAsync(
        SnapshotStreamKey streamKey,
        CancellationToken cancellationToken = default
    )
    {
        Logger.DeletingAllSnapshots(streamKey);
        await Repository.DeleteAllAsync(streamKey, cancellationToken).ConfigureAwait(false);
        Logger.AllSnapshotsDeleted(streamKey);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken = default
    )
    {
        Logger.DeletingSnapshot(snapshotKey);
        await Repository.DeleteAsync(snapshotKey, cancellationToken).ConfigureAwait(false);
        SnapshotBlobStorageMetrics.RecordDelete(snapshotKey.Stream.SnapshotStorageName);
        Logger.SnapshotDeleted(snapshotKey);
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
        int deletedCount =
            await Repository.PruneAsync(streamKey, retainModuli, cancellationToken).ConfigureAwait(false);
        SnapshotBlobStorageMetrics.RecordPrune(streamKey.SnapshotStorageName, deletedCount);
        Logger.SnapshotsPruned(streamKey, deletedCount);
    }

    /// <inheritdoc />
    public async Task<SnapshotEnvelope?> ReadAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken = default
    )
    {
        Logger.ReadingSnapshot(snapshotKey);
        Stopwatch stopwatch = Stopwatch.StartNew();
        SnapshotEnvelope? envelope = await Repository.ReadAsync(snapshotKey, cancellationToken).ConfigureAwait(false);
        stopwatch.Stop();
        bool found = envelope is not null;
        SnapshotBlobStorageMetrics.RecordRead(
            snapshotKey.Stream.SnapshotStorageName,
            stopwatch.Elapsed.TotalMilliseconds,
            found);
        if (found)
        {
            Logger.SnapshotFound(snapshotKey);
        }
        else
        {
            Logger.SnapshotNotFound(snapshotKey);
        }

        return envelope;
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
            SnapshotBlobStorageMetrics.RecordWrite(
                snapshotKey.Stream.SnapshotStorageName,
                stopwatch.Elapsed.TotalMilliseconds,
                true,
                snapshot.DataSizeBytes);
            Logger.SnapshotWritten(snapshotKey);
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            SnapshotBlobStorageMetrics.RecordWrite(
                snapshotKey.Stream.SnapshotStorageName,
                stopwatch.Elapsed.TotalMilliseconds,
                false,
                snapshot.DataSizeBytes);
            Logger.SnapshotWriteFailed(snapshotKey, exception);
            throw;
        }
    }
}