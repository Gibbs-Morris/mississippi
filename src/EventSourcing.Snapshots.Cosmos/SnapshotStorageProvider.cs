using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
    public SnapshotStorageProvider(
        ISnapshotCosmosRepository repository
    ) =>
        Repository = repository ?? throw new ArgumentNullException(nameof(repository));

    /// <inheritdoc />
    public string Format => "cosmos-db";

    private ISnapshotCosmosRepository Repository { get; }

    /// <inheritdoc />
    public Task DeleteAllAsync(
        SnapshotStreamKey streamKey,
        CancellationToken cancellationToken = default
    ) =>
        Repository.DeleteAllAsync(streamKey, cancellationToken);

    /// <inheritdoc />
    public Task DeleteAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken = default
    ) =>
        Repository.DeleteAsync(snapshotKey, cancellationToken);

    /// <inheritdoc />
    public async Task PruneAsync(
        SnapshotStreamKey streamKey,
        IReadOnlyCollection<int> retainModuli,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(retainModuli);
        await Repository.PruneAsync(streamKey, retainModuli, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<SnapshotEnvelope?> ReadAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken = default
    ) =>
        Repository.ReadAsync(snapshotKey, cancellationToken);

    /// <inheritdoc />
    public Task WriteAsync(
        SnapshotKey snapshotKey,
        SnapshotEnvelope snapshot,
        CancellationToken cancellationToken = default
    ) =>
        Repository.WriteAsync(snapshotKey, snapshot, cancellationToken);
}