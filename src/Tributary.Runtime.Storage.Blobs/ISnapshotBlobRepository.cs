using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.Tributary.Abstractions;


namespace Mississippi.Tributary.Runtime.Storage.Blobs;

/// <summary>
///     Provides Blob-backed snapshot persistence semantics behind the provider facade.
/// </summary>
internal interface ISnapshotBlobRepository
{
    /// <summary>
    ///     Deletes all snapshots for a stream.
    /// </summary>
    /// <param name="streamKey">The stream key identifying the snapshot stream.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteAllAsync(
        SnapshotStreamKey streamKey,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Deletes a specific snapshot version.
    /// </summary>
    /// <param name="snapshotKey">The snapshot key identifying the snapshot version.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Prunes snapshots for a stream.
    /// </summary>
    /// <param name="streamKey">The stream key identifying the snapshot stream.</param>
    /// <param name="retainModuli">The retention moduli.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The number of deleted snapshots.</returns>
    Task<int> PruneAsync(
        SnapshotStreamKey streamKey,
        IReadOnlyCollection<int> retainModuli,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Reads a snapshot version from Blob storage.
    /// </summary>
    /// <param name="snapshotKey">The snapshot key identifying the snapshot version.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The snapshot envelope when found; otherwise <c>null</c>.</returns>
    Task<SnapshotEnvelope?> ReadAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Writes a snapshot version to Blob storage.
    /// </summary>
    /// <param name="snapshotKey">The snapshot key identifying the snapshot version.</param>
    /// <param name="snapshot">The snapshot envelope to persist.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task WriteAsync(
        SnapshotKey snapshotKey,
        SnapshotEnvelope snapshot,
        CancellationToken cancellationToken = default
    );
}