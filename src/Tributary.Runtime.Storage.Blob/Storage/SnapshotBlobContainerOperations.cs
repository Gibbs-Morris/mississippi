using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Azure;
using Azure.Core;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


namespace Mississippi.Tributary.Runtime.Storage.Blob.Storage;

/// <summary>
///     Blob SDK implementation of <see cref="ISnapshotBlobContainerOperations" />.
/// </summary>
internal sealed class SnapshotBlobContainerOperations : ISnapshotBlobContainerOperations
{
    private const string CompressedMetadataKey = "is-compressed";

    private const string DataSizeMetadataKey = "data-size-bytes";

    /// <summary>
    ///     Initializes a new instance of the <see cref="SnapshotBlobContainerOperations" /> class.
    /// </summary>
    /// <param name="blobContainerClient">The Blob container client for snapshot storage.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    public SnapshotBlobContainerOperations(
        [FromKeyedServices(SnapshotBlobDefaults.BlobContainerServiceKey)]
        BlobContainerClient blobContainerClient,
        ILogger<SnapshotBlobContainerOperations> logger
    )
    {
        BlobContainerClient = blobContainerClient ?? throw new ArgumentNullException(nameof(blobContainerClient));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private BlobContainerClient BlobContainerClient { get; }

    private ILogger<SnapshotBlobContainerOperations> Logger { get; }

    /// <inheritdoc />
    public async Task<bool> DeleteBlobIfExistsAsync(
        string blobName,
        CancellationToken cancellationToken = default
    )
    {
        Logger.DeletingBlob(blobName);
        Response<bool> response = await BlobContainerClient.DeleteBlobIfExistsAsync(
                blobName,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        Logger.BlobDeleted(blobName, response.Value);
        return response.Value;
    }

    /// <inheritdoc />
    public async Task<SnapshotBlobDownloadResult?> DownloadBlobAsync(
        string blobName,
        CancellationToken cancellationToken = default
    )
    {
        BlobClient blobClient = BlobContainerClient.GetBlobClient(blobName);
        Logger.DownloadingBlob(blobName);
        try
        {
            Response<BlobDownloadResult> response = await blobClient.DownloadContentAsync(cancellationToken)
                .ConfigureAwait(false);
            Logger.BlobFound(blobName);
            BlobDownloadDetails details = response.Value.Details;
            long dataSizeBytes = TryParseMetadata(details.Metadata, DataSizeMetadataKey, out long parsedSize)
                ? parsedSize
                : response.Value.Content.ToMemory().Length;
            bool isCompressed =
                TryParseMetadata(details.Metadata, CompressedMetadataKey, out bool compressed) && compressed;
            return new(
                response.Value.Content.ToArray(),
                details.ContentType ?? string.Empty,
                dataSizeBytes,
                isCompressed);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            Logger.BlobNotFound(blobName);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task EnsureContainerExistsAsync(
        CancellationToken cancellationToken = default
    )
    {
        Logger.EnsuringContainerExists(BlobContainerClient.Name);
        await BlobContainerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        Logger.ContainerEnsured(BlobContainerClient.Name);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<SnapshotBlobListItem> ListBlobsAsync(
        string prefix,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        Logger.ListingBlobs(prefix);
        await foreach (BlobItem blobItem in BlobContainerClient.GetBlobsAsync(
                           BlobTraits.None,
                           BlobStates.None,
                           prefix,
                           cancellationToken))
        {
            yield return new(blobItem.Name);
        }
    }

    /// <inheritdoc />
    public async Task UploadBlobAsync(
        string blobName,
        SnapshotBlobWriteRequest request,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        BlobClient blobClient = BlobContainerClient.GetBlobClient(blobName);
        BlobUploadOptions uploadOptions = new()
        {
            HttpHeaders = new()
            {
                ContentEncoding = request.IsCompressed ? "gzip" : null,
                ContentType = request.DataContentType,
            },
            Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [CompressedMetadataKey] = request.IsCompressed ? bool.TrueString : bool.FalseString,
                [DataSizeMetadataKey] = request.DataSizeBytes.ToString(CultureInfo.InvariantCulture),
            },
        };
        Logger.UploadingBlob(blobName);
        await blobClient.UploadAsync(BinaryData.FromBytes(request.Data), uploadOptions, cancellationToken)
            .ConfigureAwait(false);
        Logger.BlobUploaded(blobName);
    }

    private static bool TryParseMetadata(
        IDictionary<string, string> metadata,
        string key,
        out bool value
    )
    {
        if (metadata.TryGetValue(key, out string? text) &&
            bool.TryParse(text, out value))
        {
            return true;
        }

        value = default;
        return false;
    }

    private static bool TryParseMetadata(
        IDictionary<string, string> metadata,
        string key,
        out long value
    )
    {
        if (metadata.TryGetValue(key, out string? text) &&
            long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }

        value = default;
        return false;
    }
}
