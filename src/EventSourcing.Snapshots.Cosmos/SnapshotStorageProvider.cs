using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Snapshots.Abstractions;
using Mississippi.EventSourcing.Snapshots.Cosmos.Abstractions;


namespace Mississippi.EventSourcing.Snapshots.Cosmos;

/// <summary>
///     Cosmos DB implementation of <see cref="ISnapshotStorageProvider" />.
/// </summary>
internal sealed class SnapshotStorageProvider : ISnapshotStorageProvider
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SnapshotStorageProvider" /> class.
    /// </summary>
    /// <param name="repository">The Cosmos repository handling snapshot persistence.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    public SnapshotStorageProvider(
        ISnapshotCosmosRepository repository,
        ILogger<SnapshotStorageProvider> logger
    )
    {
        Repository = repository ?? throw new ArgumentNullException(nameof(repository));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string Format => "cosmos-db";

    private ILogger<SnapshotStorageProvider> Logger { get; }

    private ISnapshotCosmosRepository Repository { get; }

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
    public Task DeleteAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken = default
    )
    {
        Logger.DeletingSnapshot(snapshotKey);
        return Repository.DeleteAsync(snapshotKey, cancellationToken);
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
    }

    /// <inheritdoc />
    public async Task<SnapshotEnvelope?> ReadAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken = default
    )
    {
        Logger.ReadingSnapshot(snapshotKey);
        SnapshotEnvelope? result = await Repository.ReadAsync(snapshotKey, cancellationToken);
        if (result is not null)
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
        await Repository.WriteAsync(snapshotKey, snapshot, cancellationToken);
        Logger.SnapshotWritten(snapshotKey);
    }
}