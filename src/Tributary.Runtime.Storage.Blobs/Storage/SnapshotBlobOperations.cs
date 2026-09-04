using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;


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
    /// <param name="options">The snapshot storage size limits.</param>
    public SnapshotBlobOperations(
        [FromKeyedServices(SnapshotBlobDefaults.BlobContainerClientServiceKey)]
        BlobContainerClient blobContainerClient,
        IOptions<SnapshotBlobStorageOptions> options
    )
    {
        BlobContainerClient = blobContainerClient ?? throw new ArgumentNullException(nameof(blobContainerClient));
        Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    private BlobContainerClient BlobContainerClient { get; }

    private SnapshotBlobStorageOptions Options { get; }

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
            using BlobDownloadStreamingResult result =
                await blobClient.DownloadStreamingAsync(null, cancellationToken).ConfigureAwait(false);
            Stream content = result.Content;
            long maximumDocumentSize = Options.MaximumSnapshotDocumentSizeBytes;
            if ((result.Details?.ContentLength ?? 0) > maximumDocumentSize)
            {
                throw new InvalidDataException(
                    $"Blob snapshot '{blobName}' exceeds the configured document size limit of {maximumDocumentSize} bytes.");
            }

            using MemoryStream document = new();
            byte[] buffer = new byte[81920];
            while (true)
            {
                int requestedBytes = (int)Math.Min(buffer.Length, (maximumDocumentSize - document.Length) + 1);
                int bytesRead = await content.ReadAsync(buffer.AsMemory(0, requestedBytes), cancellationToken)
                    .ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    break;
                }

                if (bytesRead > (maximumDocumentSize - document.Length))
                {
                    throw new InvalidDataException(
                        $"Blob snapshot '{blobName}' exceeds the configured document size limit of {maximumDocumentSize} bytes.");
                }

                await document.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
            }

            return new(document.ToArray());
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