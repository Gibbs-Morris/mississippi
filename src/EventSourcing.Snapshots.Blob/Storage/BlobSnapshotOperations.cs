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
using Microsoft.Extensions.Logging;

using Mississippi.Common.Abstractions;


namespace Mississippi.EventSourcing.Snapshots.Blob.Storage;

/// <summary>
///     Azure Blob Storage implementation of <see cref="IBlobSnapshotOperations" />.
/// </summary>
internal sealed class BlobSnapshotOperations : IBlobSnapshotOperations
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BlobSnapshotOperations" /> class.
    /// </summary>
    /// <param name="containerClient">The blob container client.</param>
    /// <param name="logger">The logger.</param>
    public BlobSnapshotOperations(
        [FromKeyedServices(MississippiDefaults.ServiceKeys.BlobSnapshots)]
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
    public async Task DeleteAsync(
        string blobPath,
        CancellationToken cancellationToken = default
    )
    {
        BlobClient blobClient = ContainerClient.GetBlobClient(blobPath);
        try
        {
            await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            // Already deleted, ignore
            Logger.BlobAlreadyDeleted(blobPath);
        }
    }

    /// <inheritdoc />
    public async Task DeleteBatchAsync(
        IEnumerable<string> blobPaths,
        CancellationToken cancellationToken = default
    )
    {
        List<string> paths = [.. blobPaths];
        if (paths.Count == 0)
        {
            return;
        }

        // Delete blobs in parallel with limited concurrency
        const int maxConcurrency = 10;
        using SemaphoreSlim semaphore = new(maxConcurrency);
        List<Task> deleteTasks = [];
        foreach (string path in paths)
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            deleteTasks.Add(DeleteWithSemaphoreAsync(path, semaphore, cancellationToken));
        }

        await Task.WhenAll(deleteTasks).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<BlobDownloadResult?> DownloadAsync(
        string blobPath,
        CancellationToken cancellationToken = default
    )
    {
        BlobClient blobClient = ContainerClient.GetBlobClient(blobPath);
        try
        {
            Response<BlobDownloadResult> response = await blobClient.DownloadContentAsync(cancellationToken)
                .ConfigureAwait(false);
            return response.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            Logger.BlobNotFound(blobPath);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task EnsureContainerExistsAsync(
        CancellationToken cancellationToken = default
    )
    {
        await ContainerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        Logger.ContainerEnsured(ContainerClient.Name);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> ListBlobsAsync(
        string prefix,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        await foreach (BlobItem blobItem in ContainerClient.GetBlobsAsync(
                               BlobTraits.None,
                               BlobStates.None,
                               prefix,
                               cancellationToken)
                           .ConfigureAwait(false))
        {
            yield return blobItem.Name;
        }
    }

    /// <inheritdoc />
    public async Task UploadAsync(
        string blobPath,
        byte[] data,
        IDictionary<string, string> metadata,
        AccessTier accessTier,
        CancellationToken cancellationToken = default
    )
    {
        BlobClient blobClient = ContainerClient.GetBlobClient(blobPath);
        BlobUploadOptions options = new()
        {
            Metadata = metadata,
            AccessTier = accessTier,
        };
        using MemoryStream stream = new(data);
        await blobClient.UploadAsync(stream, options, cancellationToken).ConfigureAwait(false);
    }

    private async Task DeleteWithSemaphoreAsync(
        string blobPath,
        SemaphoreSlim semaphore,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await DeleteAsync(blobPath, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            semaphore.Release();
        }
    }
}