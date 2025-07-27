using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;

using Microsoft.Extensions.Options;


namespace Mississippi.EventSourcing.Cosmos.Locking;

/// <summary>
///     Distributed lock manager implementation using Azure Blob Storage.
/// </summary>
internal class BlobDistributedLockManager : IDistributedLockManager
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BlobDistributedLockManager" /> class.
    /// </summary>
    /// <param name="blobServiceClient">The blob service client for accessing Azure Blob Storage.</param>
    /// <param name="options">The configuration options for brook storage.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public BlobDistributedLockManager(
        BlobServiceClient blobServiceClient,
        IOptions<BrookStorageOptions> options
    )
    {
        BlobServiceClient = blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient));
        Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    private BlobServiceClient BlobServiceClient { get; }

    private BrookStorageOptions Options { get; }

    /// <summary>
    ///     Acquires a distributed lock for the specified key and duration.
    /// </summary>
    /// <param name="lockKey">The unique key for the lock.</param>
    /// <param name="duration">The duration to hold the lock.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation that returns the acquired lock.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the lock cannot be acquired.</exception>
    public async Task<IDistributedLock> AcquireLockAsync(
        string lockKey,
        TimeSpan duration,
        CancellationToken cancellationToken = default
    )
    {
        BlobContainerClient? containerClient = BlobServiceClient.GetBlobContainerClient(Options.LockContainerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        BlobClient? blobClient = containerClient.GetBlobClient($"locks/{lockKey}");
        if (!await blobClient.ExistsAsync(cancellationToken))
        {
            await blobClient.UploadAsync(new BinaryData("lock"), cancellationToken);
        }

        BlobLeaseClient? leaseClient = blobClient.GetBlobLeaseClient();
        Response<BlobLease>? lease = await leaseClient.AcquireAsync(duration, cancellationToken: cancellationToken);
        return new BlobDistributedLock(
            leaseClient,
            lease.Value.LeaseId,
            Options.LeaseRenewalThresholdSeconds,
            Options.LeaseDurationSeconds);
    }
}