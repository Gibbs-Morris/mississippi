using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.Tributary.Runtime.Storage.Blobs.Storage;

/// <summary>
///     Azure Blob SDK implementation of <see cref="ISnapshotBlobOperations" />.
/// </summary>
internal sealed class SnapshotBlobOperations : ISnapshotBlobOperations
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SnapshotBlobOperations" /> class.
    /// </summary>
    /// <param name="blobContainerClient">The keyed Blob container client for snapshot storage.</param>
    public SnapshotBlobOperations(
        [FromKeyedServices(SnapshotBlobDefaults.BlobContainerClientServiceKey)]
        BlobContainerClient blobContainerClient
    ) =>
        BlobContainerClient = blobContainerClient ?? throw new ArgumentNullException(nameof(blobContainerClient));

    private BlobContainerClient BlobContainerClient { get; }

    /// <inheritdoc />
    public async Task CreateContainerIfNotExistsAsync(
        CancellationToken cancellationToken = default
    ) =>
        _ = await BlobContainerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<bool> DeleteIfExistsAsync(
        string blobName,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(blobName);
        Response<bool> response = await BlobContainerClient
            .DeleteBlobIfExistsAsync(blobName, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return response.Value;
    }

    /// <inheritdoc />
    public async Task<BinaryData?> DownloadAsync(
        string blobName,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(blobName);
        BlobClient blobClient = BlobContainerClient.GetBlobClient(blobName);
        try
        {
            BlobDownloadResult result = await blobClient.DownloadContentAsync(cancellationToken).ConfigureAwait(false);
            return result.Content;
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> ListBlobNamesAsync(
        string prefix,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        await foreach (BlobItem blobItem in BlobContainerClient.GetBlobsAsync(
                           BlobTraits.None,
                           BlobStates.None,
                           prefix,
                           cancellationToken))
        {
            yield return blobItem.Name;
        }
    }

    /// <inheritdoc />
    public async Task UploadAsync(
        string blobName,
        BinaryData document,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(blobName);
        ArgumentNullException.ThrowIfNull(document);
        BlobClient blobClient = BlobContainerClient.GetBlobClient(blobName);
        await blobClient.UploadAsync(
                document,
                new BlobUploadOptions
                {
                    HttpHeaders = new()
                    {
                        ContentType = "application/json",
                    },
                },
                cancellationToken)
            .ConfigureAwait(false);
    }
}