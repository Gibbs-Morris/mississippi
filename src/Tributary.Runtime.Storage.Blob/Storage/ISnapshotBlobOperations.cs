using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.Tributary.Runtime.Storage.Blob.Storage;

/// <summary>
///     Encapsulates the Azure Blob SDK operations required by the snapshot provider.
/// </summary>
internal interface ISnapshotBlobOperations
{
    /// <summary>
    ///     Uploads a Blob only when no Blob already exists at the same name.
    /// </summary>
    /// <param name="blobName">The Blob name to create.</param>
    /// <param name="content">The payload stream to upload.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true" /> when the Blob was created; otherwise, <see langword="false" /> for a duplicate.</returns>
    Task<bool> CreateIfAbsentAsync(
        string blobName,
        Stream content,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Lists Blob names for a stream-local prefix in incremental pages.
    /// </summary>
    /// <param name="prefix">The stream-local prefix.</param>
    /// <param name="pageSizeHint">The requested page size hint.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An asynchronous sequence of Blob-name pages.</returns>
    IAsyncEnumerable<SnapshotBlobPage> ListByPrefixAsync(
        string prefix,
        int pageSizeHint,
        CancellationToken cancellationToken = default
    );
}