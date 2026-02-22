using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.EventSourcing.Snapshots.Abstractions;


namespace Mississippi.EventSourcing.Snapshots.Cosmos;

/// <summary>
///     Defines Cosmos-backed snapshot persistence operations.
/// </summary>
internal interface ISnapshotCosmosRepository
{
    /// <summary>
    ///     Deletes all snapshots for a stream.
    /// </summary>
    /// <param name="streamKey">The snapshot stream identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that completes when deletion finishes.</returns>
    Task DeleteAllAsync(
        SnapshotStreamKey streamKey,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Deletes a specific snapshot.
    /// </summary>
    /// <param name="snapshotKey">The snapshot identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that completes when the snapshot is removed.</returns>
    Task DeleteAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Prunes snapshots for a stream using modulus retention rules.
    /// </summary>
    /// <param name="streamKey">The snapshot stream identifier.</param>
    /// <param name="retainModuli">Modulus values to retain.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the pruning operation.</returns>
    Task PruneAsync(
        SnapshotStreamKey streamKey,
        IReadOnlyCollection<int> retainModuli,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Reads a snapshot envelope by key.
    /// </summary>
    /// <param name="snapshotKey">The snapshot identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The snapshot envelope if it exists; otherwise null.</returns>
    Task<SnapshotEnvelope?> ReadAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Writes a snapshot envelope.
    /// </summary>
    /// <param name="snapshotKey">The snapshot identifier.</param>
    /// <param name="snapshot">The snapshot envelope to persist.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous write.</returns>
    Task WriteAsync(
        SnapshotKey snapshotKey,
        SnapshotEnvelope snapshot,
        CancellationToken cancellationToken = default
    );
}