using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Azure.Storage.Blobs.Models;


namespace Mississippi.EventSourcing.Snapshots.Blob.Storage;

/// <summary>
///     Low-level blob operations for snapshot storage.
/// </summary>
internal interface IBlobSnapshotOperations
{
    /// <summary>
    ///     Deletes a blob.
    /// </summary>
    /// <param name="blobPath">The blob path.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when the deletion finishes.</returns>
    Task DeleteAsync(
        string blobPath,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Deletes multiple blobs in a batch.
    /// </summary>
    /// <param name="blobPaths">The blob paths to delete.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when all deletions finish.</returns>
    Task DeleteBatchAsync(
        IEnumerable<string> blobPaths,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Downloads a blob's content and properties.
    /// </summary>
    /// <param name="blobPath">The blob path.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The download result, or null if the blob does not exist.</returns>
    Task<BlobDownloadResult?> DownloadAsync(
        string blobPath,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Ensures the container exists.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when the container is confirmed to exist.</returns>
    Task EnsureContainerExistsAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Lists blobs matching a prefix.
    /// </summary>
    /// <param name="prefix">The prefix to match.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>An async enumerable of blob paths.</returns>
    IAsyncEnumerable<string> ListBlobsAsync(
        string prefix,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Uploads data to a blob with metadata and access tier.
    /// </summary>
    /// <param name="blobPath">The blob path.</param>
    /// <param name="data">The data to upload.</param>
    /// <param name="metadata">Metadata to attach to the blob.</param>
    /// <param name="accessTier">The access tier for the blob.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when the upload finishes.</returns>
    Task UploadAsync(
        string blobPath,
        byte[] data,
        IDictionary<string, string> metadata,
        AccessTier accessTier,
        CancellationToken cancellationToken = default
    );
}