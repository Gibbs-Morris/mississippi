using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.Tributary.Abstractions;


namespace Mississippi.Tributary.Runtime.Storage.Azure.Storage;

/// <summary>
///     Provides Azure Blob persistence operations for Tributary snapshot storage.
/// </summary>
internal interface IAzureSnapshotRepository
{
    /// <summary>
    ///     Deletes every snapshot blob belonging to the specified stream.
    /// </summary>
    /// <param name="streamKey">The snapshot stream to delete.</param>
    /// <param name="cancellationToken">The operation cancellation token.</param>
    /// <returns>A task that represents the asynchronous delete-all operation.</returns>
    Task DeleteAllAsync(
        SnapshotStreamKey streamKey,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Deletes the specified snapshot blob when it exists.
    /// </summary>
    /// <param name="snapshotKey">The snapshot key to delete.</param>
    /// <param name="cancellationToken">The operation cancellation token.</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    Task DeleteAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Prunes snapshots for the specified stream using the supplied retention moduli.
    /// </summary>
    /// <param name="streamKey">The snapshot stream to prune.</param>
    /// <param name="retainModuli">The moduli that determine which versions are retained.</param>
    /// <param name="cancellationToken">The operation cancellation token.</param>
    /// <returns>A task that represents the asynchronous prune operation.</returns>
    Task PruneAsync(
        SnapshotStreamKey streamKey,
        IReadOnlyCollection<int> retainModuli,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Reads the exact snapshot identified by the supplied key.
    /// </summary>
    /// <param name="snapshotKey">The snapshot key to read.</param>
    /// <param name="cancellationToken">The operation cancellation token.</param>
    /// <returns>The matching snapshot envelope, or <c>null</c> when no blob exists.</returns>
    Task<SnapshotEnvelope?> ReadAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Writes the supplied snapshot blob.
    /// </summary>
    /// <param name="snapshotKey">The snapshot key to write.</param>
    /// <param name="snapshot">The snapshot envelope to persist.</param>
    /// <param name="cancellationToken">The operation cancellation token.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    Task WriteAsync(
        SnapshotKey snapshotKey,
        SnapshotEnvelope snapshot,
        CancellationToken cancellationToken = default
    );
}
