using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.Tributary.Runtime.Storage.Blobs;

/// <summary>
///     Abstracts Azure Blob SDK operations for snapshot storage.
/// </summary>
internal interface ISnapshotBlobOperations
{
    /// <summary>
    ///     Creates the configured snapshot container when it does not already exist.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CreateContainerIfNotExistsAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Deletes a Blob when it exists.
    /// </summary>
    /// <param name="blobName">The Blob name to delete.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><c>true</c> when the Blob existed and was deleted; otherwise <c>false</c>.</returns>
    Task<bool> DeleteIfExistsAsync(
        string blobName,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Downloads a snapshot document Blob.
    /// </summary>
    /// <param name="blobName">The Blob name to download.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The Blob content when found; otherwise <c>null</c>.</returns>
    Task<BinaryData?> DownloadAsync(
        string blobName,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Lists Blob names under a stream-scoped prefix.
    /// </summary>
    /// <param name="prefix">The Blob prefix to list.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>An asynchronous sequence of Blob names.</returns>
    IAsyncEnumerable<string> ListBlobNamesAsync(
        string prefix,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Uploads a snapshot document Blob.
    /// </summary>
    /// <param name="blobName">The destination Blob name.</param>
    /// <param name="document">The serialized JSON document to upload.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UploadAsync(
        string blobName,
        BinaryData document,
        CancellationToken cancellationToken = default
    );
}