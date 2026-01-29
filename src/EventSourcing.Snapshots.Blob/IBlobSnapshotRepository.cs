using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.EventSourcing.Snapshots.Abstractions;


namespace Mississippi.EventSourcing.Snapshots.Blob;

/// <summary>
///     Domain-level repository interface for blob snapshot operations.
/// </summary>
internal interface IBlobSnapshotRepository
{
    /// <summary>
    ///     Deletes all snapshots for a stream.
    /// </summary>
    /// <param name="streamKey">The stream key.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when all snapshots are deleted.</returns>
    Task DeleteAllAsync(
        SnapshotStreamKey streamKey,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Deletes a specific snapshot version.
    /// </summary>
    /// <param name="snapshotKey">The snapshot key.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when the snapshot is deleted.</returns>
    Task DeleteAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Prunes snapshots for a stream, retaining versions divisible by any provided modulus.
    /// </summary>
    /// <param name="streamKey">The stream key.</param>
    /// <param name="retainModuli">Moduli for retention (versions divisible by any value are kept).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when pruning finishes.</returns>
    Task PruneAsync(
        SnapshotStreamKey streamKey,
        IReadOnlyCollection<int> retainModuli,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Reads a snapshot from blob storage.
    /// </summary>
    /// <param name="snapshotKey">The snapshot key.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The snapshot envelope, or null if not found.</returns>
    Task<SnapshotEnvelope?> ReadAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Writes a snapshot to blob storage.
    /// </summary>
    /// <param name="snapshotKey">The snapshot key.</param>
    /// <param name="envelope">The snapshot envelope.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when the snapshot is written.</returns>
    Task WriteAsync(
        SnapshotKey snapshotKey,
        SnapshotEnvelope envelope,
        CancellationToken cancellationToken = default
    );
}