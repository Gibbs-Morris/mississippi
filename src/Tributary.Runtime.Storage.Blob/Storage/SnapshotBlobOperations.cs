using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.Tributary.Runtime.Storage.Blob.Storage;

/// <summary>
///     Azure Blob SDK implementation of the increment-2 Blob operations seam.
/// </summary>
internal sealed class SnapshotBlobOperations : ISnapshotBlobOperations
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SnapshotBlobOperations" /> class.
    /// </summary>
    /// <param name="blobContainerClient">The keyed Blob container client.</param>
    public SnapshotBlobOperations(
        [FromKeyedServices(SnapshotBlobDefaults.BlobContainerServiceKey)] BlobContainerClient blobContainerClient
    ) =>
        BlobContainerClient = blobContainerClient ?? throw new ArgumentNullException(nameof(blobContainerClient));

    private BlobContainerClient BlobContainerClient { get; }

    /// <inheritdoc />
    public async Task<bool> CreateIfAbsentAsync(
        string blobName,
        System.IO.Stream content,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);
        ArgumentNullException.ThrowIfNull(content);

        BlobClient blobClient = BlobContainerClient.GetBlobClient(blobName);

        try
        {
            await blobClient.UploadAsync(
                    content,
                    new BlobUploadOptions
                    {
                        Conditions = new BlobRequestConditions
                        {
                            IfNoneMatch = ETag.All,
                        },
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            return true;
        }
        catch (RequestFailedException ex) when ((ex.Status == 409) || (ex.Status == 412))
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteIfExistsAsync(
        string blobName,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);

        BlobClient blobClient = BlobContainerClient.GetBlobClient(blobName);
        Response<bool> response = await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return response.Value;
    }

    /// <inheritdoc />
    public async Task<byte[]?> DownloadIfExistsAsync(
        string blobName,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);

        BlobClient blobClient = BlobContainerClient.GetBlobClient(blobName);

        try
        {
            Response<BlobDownloadResult> response = await blobClient.DownloadContentAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return response.Value.Content.ToArray();
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public IAsyncEnumerable<SnapshotBlobPage> ListByPrefixAsync(
        string prefix,
        int pageSizeHint,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(prefix);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSizeHint);

        return ListByPrefixCoreAsync(prefix, pageSizeHint, cancellationToken);
    }

    private async IAsyncEnumerable<SnapshotBlobPage> ListByPrefixCoreAsync(
        string prefix,
        int pageSizeHint,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        AsyncPageable<BlobItem> blobs = BlobContainerClient.GetBlobsAsync(
            traits: BlobTraits.None,
            states: BlobStates.None,
            prefix: prefix,
            cancellationToken: cancellationToken);

        await foreach (Page<BlobItem> page in blobs.AsPages(default, pageSizeHint).WithCancellation(cancellationToken))
        {
            yield return new SnapshotBlobPage(page.Values.Select(static blobItem => blobItem.Name).ToArray());
        }
    }
}