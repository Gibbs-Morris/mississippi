using System.Security.Cryptography;

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

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
    /// <param name="leaseClientFactory">Factory used to create blob lease clients.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public BlobDistributedLockManager(
        BlobServiceClient blobServiceClient,
        IOptions<BrookStorageOptions> options,
        IBlobLeaseClientFactory leaseClientFactory
    )
    {
        BlobServiceClient = blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient));
        Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        LeaseClientFactory = leaseClientFactory ?? throw new ArgumentNullException(nameof(leaseClientFactory));
    }

    private BlobServiceClient BlobServiceClient { get; }

    private BrookStorageOptions Options { get; }

    private IBlobLeaseClientFactory LeaseClientFactory { get; }

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

        IBlobLeaseClient leaseClient = LeaseClientFactory.Create(blobClient);

        // Bounded retries for lease conflicts
        const int maxAcquireAttempts = 5;
        Response<BlobLease>? lease = null;
        for (int attempt = 0; attempt < maxAcquireAttempts; attempt++)
        {
            try
            {
                lease = await leaseClient.AcquireAsync(duration, cancellationToken: cancellationToken);
                break;
            }
            catch (RequestFailedException ex) when (ex.Status == 409)
            {
                // Lease is already held; backoff with jitter and retry while attempts remain
                if (attempt < (maxAcquireAttempts - 1))
                {
                    int backoffMs = (int)Math.Min(2000, Math.Pow(2, attempt) * 100);
                    int jitter = RandomNumberGenerator.GetInt32(0, 100);
                    await Task.Delay(backoffMs + jitter, cancellationToken);
                }
            }
        }

        if (lease is null)
        {
            throw new InvalidOperationException("Failed to acquire blob lease for distributed lock after retries.");
        }

        return new BlobDistributedLock(
            leaseClient,
            lease.Value.LeaseId,
            Options.LeaseRenewalThresholdSeconds,
            Options.LeaseDurationSeconds);
    }
}