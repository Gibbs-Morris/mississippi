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
    private readonly ISnapshotCosmosRepository repository;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SnapshotStorageProvider" /> class.
    /// </summary>
    /// <param name="repository">The Cosmos repository handling snapshot persistence.</param>
    public SnapshotStorageProvider(
        ISnapshotCosmosRepository repository
    ) =>
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));

    /// <inheritdoc />
    public string Format => "cosmos-db";

    /// <inheritdoc />
    public Task DeleteAllAsync(
        SnapshotStreamKey streamKey,
        CancellationToken cancellationToken = default
    ) =>
        repository.DeleteAllAsync(streamKey, cancellationToken);

    /// <inheritdoc />
    public Task DeleteAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken = default
    ) =>
        repository.DeleteAsync(snapshotKey, cancellationToken);

    /// <inheritdoc />
    public async Task PruneAsync(
        SnapshotStreamKey streamKey,
        IReadOnlyCollection<int> retainModuli,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(retainModuli);
        await repository.PruneAsync(streamKey, retainModuli, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<SnapshotEnvelope?> ReadAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken = default
    ) =>
        repository.ReadAsync(snapshotKey, cancellationToken);

    /// <inheritdoc />
    public Task WriteAsync(
        SnapshotKey snapshotKey,
        SnapshotEnvelope snapshot,
        CancellationToken cancellationToken = default
    ) =>
        repository.WriteAsync(snapshotKey, snapshot, cancellationToken);
}