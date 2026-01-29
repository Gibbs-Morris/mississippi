using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Azure.Storage.Blobs.Models;

using Mississippi.EventSourcing.Snapshots.Abstractions;


namespace Mississippi.EventSourcing.Snapshots.Blob.Storage;

/// <summary>
///     Abstraction for Azure Blob Storage operations on snapshots, enabling testability.
/// </summary>
internal interface IBlobSnapshotOperations
{
    /// <summary>
    ///     Deletes all blobs matching the stream prefix.
    /// </summary>
    /// <param name="streamKey">The stream key to build the prefix.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when all blobs are deleted.</returns>
    Task DeleteAllAsync(
        SnapshotStreamKey streamKey,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Deletes a single blob by snapshot key.
    /// </summary>
    /// <param name="snapshotKey">The snapshot key identifying the blob.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the blob is deleted.</returns>
    Task DeleteAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Lists all blob names matching the stream prefix.
    /// </summary>
    /// <param name="streamKey">The stream key to build the prefix.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of blob names.</returns>
    IAsyncEnumerable<string> ListBlobNamesAsync(
        SnapshotStreamKey streamKey,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Reads a snapshot blob and returns the envelope.
    /// </summary>
    /// <param name="snapshotKey">The snapshot key identifying the blob.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The snapshot envelope, or <c>null</c> if not found.</returns>
    Task<SnapshotEnvelope?> ReadAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Writes a snapshot envelope to a blob with optional access tier.
    /// </summary>
    /// <param name="snapshotKey">The snapshot key identifying the blob.</param>
    /// <param name="envelope">The snapshot envelope to write.</param>
    /// <param name="accessTier">The blob access tier (Hot, Cool, Cold); Archive is not supported.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the blob is written.</returns>
    Task WriteAsync(
        SnapshotKey snapshotKey,
        SnapshotEnvelope envelope,
        AccessTier? accessTier,
        CancellationToken cancellationToken
    );
}