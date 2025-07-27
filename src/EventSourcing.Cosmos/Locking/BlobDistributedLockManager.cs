using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Options;

namespace Mississippi.EventSourcing.Cosmos.Locking;

internal class BlobDistributedLockManager : IDistributedLockManager
{
    private BlobServiceClient BlobServiceClient { get; }
    private BrookStorageOptions Options { get; }

    public BlobDistributedLockManager(BlobServiceClient blobServiceClient, IOptions<BrookStorageOptions> options)
    {
        BlobServiceClient = blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient));
        Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<IDistributedLock> AcquireLockAsync(string lockKey, TimeSpan duration, CancellationToken cancellationToken = default)
    {
        var containerClient = BlobServiceClient.GetBlobContainerClient(Options.LockContainerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blobClient = containerClient.GetBlobClient($"locks/{lockKey}");

        if (!await blobClient.ExistsAsync(cancellationToken))
        {
            await blobClient.UploadAsync(new BinaryData("lock"), cancellationToken: cancellationToken);
        }

        var leaseClient = blobClient.GetBlobLeaseClient();
        var lease = await leaseClient.AcquireAsync(duration, cancellationToken: cancellationToken);

        return new BlobDistributedLock(leaseClient, lease.Value.LeaseId, Options.LeaseRenewalThresholdSeconds, Options.LeaseDurationSeconds);
    }
}