using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.Tributary.Runtime.Storage.Blob.Storage;


namespace Mississippi.Tributary.Runtime.Storage.Blob;

/// <summary>
///     Defines direct Blob container operations used by snapshot persistence.
/// </summary>
internal interface ISnapshotBlobContainerOperations
{
    /// <summary>
    ///     Deletes a Blob when it exists.
    /// </summary>
    /// <param name="blobName">The Blob name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true" /> when the Blob existed and was deleted.</returns>
    Task<bool> DeleteBlobIfExistsAsync(
        string blobName,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Downloads a Blob snapshot payload by name.
    /// </summary>
    /// <param name="blobName">The Blob name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The download result if the Blob exists; otherwise <see langword="null" />.</returns>
    Task<SnapshotBlobDownloadResult?> DownloadBlobAsync(
        string blobName,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Ensures the target container exists.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task EnsureContainerExistsAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Lists Blob snapshot items under the supplied prefix.
    /// </summary>
    /// <param name="prefix">The Blob prefix.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An asynchronous sequence of Blob list items.</returns>
    IAsyncEnumerable<SnapshotBlobListItem> ListBlobsAsync(
        string prefix,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Uploads a Blob snapshot payload.
    /// </summary>
    /// <param name="blobName">The Blob name.</param>
    /// <param name="request">The write request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UploadBlobAsync(
        string blobName,
        SnapshotBlobWriteRequest request,
        CancellationToken cancellationToken = default
    );
}
