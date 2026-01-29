using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Snapshots.Abstractions;


namespace Mississippi.EventSourcing.Snapshots.Blob.Storage;

/// <summary>
///     Concrete implementation of <see cref="IBlobSnapshotOperations" /> using Azure Blob Storage SDK.
/// </summary>
internal sealed class BlobSnapshotOperations : IBlobSnapshotOperations
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BlobSnapshotOperations" /> class.
    /// </summary>
    /// <param name="containerClient">The blob container client for snapshot operations.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    public BlobSnapshotOperations(
        BlobContainerClient containerClient,
        ILogger<BlobSnapshotOperations> logger
    )
    {
        ContainerClient = containerClient ?? throw new ArgumentNullException(nameof(containerClient));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private BlobContainerClient ContainerClient { get; }

    private ILogger<BlobSnapshotOperations> Logger { get; }

    /// <inheritdoc />
    public async Task DeleteAllAsync(
        SnapshotStreamKey streamKey,
        CancellationToken cancellationToken
    )
    {
        string prefix = BlobPathBuilder.BuildStreamPrefix(streamKey);
        Logger.DeletingAllBlobsWithPrefix(prefix);
        await foreach (BlobItem blobItem in ContainerClient.GetBlobsAsync(
                           BlobTraits.None,
                           BlobStates.None,
                           prefix,
                           cancellationToken))
        {
            await ContainerClient.DeleteBlobIfExistsAsync(blobItem.Name, cancellationToken: cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken
    )
    {
        string blobPath = BlobPathBuilder.BuildPath(snapshotKey);
        Logger.DeletingBlob(blobPath);
        await ContainerClient.DeleteBlobIfExistsAsync(blobPath, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> ListBlobNamesAsync(
        SnapshotStreamKey streamKey,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        string prefix = BlobPathBuilder.BuildStreamPrefix(streamKey);
        await foreach (BlobItem blobItem in ContainerClient.GetBlobsAsync(
                           BlobTraits.None,
                           BlobStates.None,
                           prefix,
                           cancellationToken))
        {
            yield return blobItem.Name;
        }
    }

    /// <inheritdoc />
    public async Task<SnapshotEnvelope?> ReadAsync(
        SnapshotKey snapshotKey,
        CancellationToken cancellationToken
    )
    {
        string blobPath = BlobPathBuilder.BuildPath(snapshotKey);
        BlobClient blobClient = ContainerClient.GetBlobClient(blobPath);
        try
        {
            BlobDownloadResult downloadResult = await blobClient.DownloadContentAsync(cancellationToken);
            BinaryData content = downloadResult.Content;
            IDictionary<string, string> metadata = downloadResult.Details.Metadata;
            string? contentType = metadata.TryGetValue("DataContentType", out string? ct)
                ? ct
                : "application/octet-stream";
            string? reducersHash = metadata.TryGetValue("ReducerHash", out string? rh) ? rh : string.Empty;
            byte[] dataBytes = content.ToArray();
            return new()
            {
                Data = ImmutableArray.Create(dataBytes),
                DataContentType = contentType,
                DataSizeBytes = dataBytes.Length,
                ReducerHash = reducersHash,
            };
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            Logger.BlobNotFound(blobPath);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task WriteAsync(
        SnapshotKey snapshotKey,
        SnapshotEnvelope envelope,
        AccessTier? accessTier,
        CancellationToken cancellationToken
    )
    {
        if (accessTier == AccessTier.Archive)
        {
            throw new ArgumentException(
                "Archive access tier is not supported for snapshot storage.",
                nameof(accessTier));
        }

        string blobPath = BlobPathBuilder.BuildPath(snapshotKey);
        BlobClient blobClient = ContainerClient.GetBlobClient(blobPath);
        byte[] dataBytes = envelope.Data.ToArray();
        BinaryData content = new(dataBytes);
        Dictionary<string, string> metadata = new()
        {
            ["DataContentType"] = envelope.DataContentType,
            ["ReducerHash"] = envelope.ReducerHash,
        };
        BlobUploadOptions uploadOptions = new()
        {
            Metadata = metadata,
            AccessTier = accessTier,
        };
        Logger.WritingBlob(blobPath, dataBytes.Length);
        await blobClient.UploadAsync(content, uploadOptions, cancellationToken);
    }
}