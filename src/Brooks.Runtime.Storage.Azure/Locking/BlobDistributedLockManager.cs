using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;

using Microsoft.Extensions.Options;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Runtime.Storage.Azure.Storage;


namespace Mississippi.Brooks.Runtime.Storage.Azure.Locking;

/// <summary>
///     Acquires Azure Blob leases for Brooks event-stream coordination.
/// </summary>
internal sealed class BlobDistributedLockManager : IDistributedLockManager
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BlobDistributedLockManager" /> class.
    /// </summary>
    /// <param name="blobServiceClient">The Azure Blob service client.</param>
    /// <param name="options">The Brooks Azure storage options.</param>
    /// <param name="streamPathEncoder">The stream path encoder.</param>
    /// <param name="timeProvider">The time provider used for backoff-sensitive logic.</param>
    public BlobDistributedLockManager(
        BlobServiceClient blobServiceClient,
        IOptions<BrookStorageOptions> options,
        IStreamPathEncoder streamPathEncoder,
        TimeProvider? timeProvider = null
    )
    {
        BlobServiceClient = blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient));
        Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        StreamPathEncoder = streamPathEncoder ?? throw new ArgumentNullException(nameof(streamPathEncoder));
        TimeProvider = timeProvider ?? TimeProvider.System;
    }

    private BlobServiceClient BlobServiceClient { get; }

    private BrookStorageOptions Options { get; }

    private IStreamPathEncoder StreamPathEncoder { get; }

    private TimeProvider TimeProvider { get; }

    /// <inheritdoc />
    public async Task<IDistributedLock> AcquireLockAsync(
        BrookKey brookId,
        TimeSpan duration,
        CancellationToken cancellationToken = default
    )
    {
        BlobContainerClient containerClient = BlobServiceClient.GetBlobContainerClient(Options.LockContainerName);
        BlobClient blobClient = containerClient.GetBlobClient(StreamPathEncoder.GetLockPath(brookId));

        if (!await blobClient.ExistsAsync(cancellationToken).ConfigureAwait(false))
        {
            try
            {
                await blobClient.UploadAsync(
                        BinaryData.FromString("lock"),
                        new BlobUploadOptions
                        {
                            Conditions = new BlobRequestConditions
                            {
                                IfNoneMatch = ETag.All,
                            },
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (RequestFailedException exception) when ((exception.Status == 409) || (exception.Status == 412))
            {
                _ = exception;
            }
        }

        BlobLeaseClient leaseClient = blobClient.GetBlobLeaseClient();
        for (int attempt = 0; attempt < 5; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                Response<BlobLease> lease = await leaseClient.AcquireAsync(duration, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                return new BlobDistributedLock(
                    leaseClient,
                    lease.Value.LeaseId,
                    Options.LeaseDurationSeconds,
                    Options.LeaseRenewalThresholdSeconds,
                    TimeProvider);
            }
            catch (RequestFailedException exception) when ((exception.Status == 409) || (exception.Status == 412))
            {
                if (attempt == 4)
                {
                    throw new BrookStorageRetryableException(
                        $"Brooks Azure could not acquire the stream lease for brook '{brookId}'. Re-read committed state and retry.",
                        exception);
                }

                int backoffMs = (int)Math.Min(1000, Math.Pow(2, attempt) * 100);
                int jitterMs = RandomNumberGenerator.GetInt32(0, 50);
                await Task.Delay(backoffMs + jitterMs, cancellationToken).ConfigureAwait(false);
            }
        }

        throw new BrookStorageRetryableException(
            $"Brooks Azure could not acquire the stream lease for brook '{brookId}'. Re-read committed state and retry.");
    }
}