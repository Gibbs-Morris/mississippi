using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.Common.Abstractions;


namespace Mississippi.EventSourcing.Brooks.Cosmos.Locking;

/// <summary>
///     Distributed lock manager implementation using Azure Blob Storage.
/// </summary>
internal sealed class BlobDistributedLockManager : IDistributedLockManager
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BlobDistributedLockManager" /> class.
    /// </summary>
    /// <param name="blobServiceClient">The blob service client for accessing Azure Blob Storage.</param>
    /// <param name="options">The configuration options for brook storage.</param>
    /// <param name="leaseClientFactory">Factory used to create blob lease clients.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public BlobDistributedLockManager(
        [FromKeyedServices(MississippiDefaults.ServiceKeys.BlobLocking)]
        BlobServiceClient blobServiceClient,
        IOptions<BrookStorageOptions> options,
        IBlobLeaseClientFactory leaseClientFactory,
        ILogger<BlobDistributedLockManager> logger
    )
    {
        BlobServiceClient = blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient));
        Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        LeaseClientFactory = leaseClientFactory ?? throw new ArgumentNullException(nameof(leaseClientFactory));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private BlobServiceClient BlobServiceClient { get; }

    private IBlobLeaseClientFactory LeaseClientFactory { get; }

    private ILogger<BlobDistributedLockManager> Logger { get; }

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
        Stopwatch stopwatch = Stopwatch.StartNew();
        Logger.AcquiringLock(lockKey, duration.TotalSeconds);
        BlobContainerClient? containerClient = BlobServiceClient.GetBlobContainerClient(Options.LockContainerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        BlobClient? blobClient = containerClient.GetBlobClient($"locks/{lockKey}");
        if (!await blobClient.ExistsAsync(cancellationToken))
        {
            Logger.CreatingLockBlob(lockKey);
            await blobClient.UploadAsync(new BinaryData("lock"), cancellationToken);
        }

        IBlobLeaseClient leaseClient = LeaseClientFactory.Create(blobClient);

        // Bounded retries for lease conflicts
        const int maxAcquireAttempts = 5;
        Response<BlobLease>? lease = null;
        int attemptsMade = 0;
        for (int attempt = 0; attempt < maxAcquireAttempts; attempt++)
        {
            attemptsMade = attempt + 1;
            try
            {
                lease = await leaseClient.AcquireAsync(duration, cancellationToken: cancellationToken);
                Logger.LockAcquired(lockKey, lease.Value.LeaseId, attempt);
                break;
            }
            catch (RequestFailedException ex) when (ex.Status == 409)
            {
                // Record contention wait for each 409 conflict
                LockMetrics.RecordContentionWait(lockKey);

                // Lease is already held; backoff with jitter and retry while attempts remain
                if (attempt < (maxAcquireAttempts - 1))
                {
                    int backoffMs = (int)Math.Min(2000, Math.Pow(2, attempt) * 100);
                    int jitter = RandomNumberGenerator.GetInt32(0, 100);
                    Logger.LockConflict(lockKey, attempt, backoffMs + jitter);
                    await Task.Delay(backoffMs + jitter, cancellationToken);
                }
            }
        }

        stopwatch.Stop();
        if (lease is null)
        {
            LockMetrics.RecordAcquireFailure(lockKey, stopwatch.Elapsed.TotalMilliseconds, attemptsMade);
            Logger.LockAcquisitionFailed(lockKey, maxAcquireAttempts);
            throw new InvalidOperationException("Failed to acquire blob lease for distributed lock after retries.");
        }

        LockMetrics.RecordAcquireSuccess(lockKey, stopwatch.Elapsed.TotalMilliseconds, attemptsMade);
        return new BlobDistributedLock(
            leaseClient,
            lease.Value.LeaseId,
            Options.LeaseRenewalThresholdSeconds,
            Options.LeaseDurationSeconds,
            lockKey,
            stopwatch);
    }
}