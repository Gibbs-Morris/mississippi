using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.Tributary.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Blob.Storage;


namespace Mississippi.Tributary.Runtime.Storage.Blob;

/// <summary>
///     Blob-backed implementation placeholder for <see cref="ISnapshotStorageProvider" />.
/// </summary>
internal sealed class SnapshotBlobStorageProvider : ISnapshotStorageProvider
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SnapshotBlobStorageProvider" /> class.
    /// </summary>
    /// <param name="repository">The Blob repository handling snapshot persistence.</param>
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
        Logger.DeletedAllSnapshots(streamKey);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken = default
    )
    {
        Logger.DeletingSnapshot(snapshotKey);
        await Repository.DeleteAsync(snapshotKey, cancellationToken).ConfigureAwait(false);
        Logger.DeletedSnapshot(snapshotKey);
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
        Logger.PrunedSnapshots(streamKey);
    }

    /// <inheritdoc />
    public async Task<SnapshotEnvelope?> ReadAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken = default
    )
    {
        Logger.ReadingSnapshot(snapshotKey);
        try
        {
            SnapshotEnvelope? snapshot =
                await Repository.ReadAsync(snapshotKey, cancellationToken).ConfigureAwait(false);
            if (snapshot is null)
            {
                Logger.SnapshotNotFound(snapshotKey);
                return null;
            }

            Logger.SnapshotFound(snapshotKey);
            return snapshot;
        }
        catch (SnapshotBlobUnreadableFrameException exception)
        {
            Logger.UnreadableSnapshotBlob(snapshotKey, exception.Reason, exception);
            throw;
        }
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
        try
        {
            await Repository.WriteAsync(snapshotKey, snapshot, cancellationToken).ConfigureAwait(false);
            Logger.SnapshotWritten(snapshotKey);
        }
        catch (SnapshotBlobDuplicateVersionException exception)
        {
            Logger.SnapshotWriteConflict(snapshotKey, exception);
            throw;
        }
    }
}