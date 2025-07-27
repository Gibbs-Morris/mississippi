using Azure.Storage.Blobs.Specialized;

namespace Mississippi.EventSourcing.Cosmos.Locking;

internal class BlobDistributedLock : IDistributedLock
{
    private BlobLeaseClient LeaseClient { get; }
    private int LeaseRenewalThresholdSeconds { get; }
    private int LeaseDurationSeconds { get; }
    private DateTime _lastRenewalTime;
    private bool _disposed;

    public BlobDistributedLock(BlobLeaseClient leaseClient, string lockId, int leaseRenewalThresholdSeconds, int leaseDurationSeconds)
    {
        LeaseClient = leaseClient;
        LockId = lockId;
        LeaseRenewalThresholdSeconds = leaseRenewalThresholdSeconds;
        LeaseDurationSeconds = leaseDurationSeconds;
        _lastRenewalTime = DateTime.UtcNow;
    }

    public string LockId { get; }

    public async Task RenewAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(BlobDistributedLock));

        var timeSinceLastRenewal = DateTime.UtcNow - _lastRenewalTime;
        var renewalThreshold = TimeSpan.FromSeconds(LeaseDurationSeconds - LeaseRenewalThresholdSeconds);

        if (timeSinceLastRenewal < renewalThreshold)
        {
            return;
        }

        try
        {
            await LeaseClient.RenewAsync(cancellationToken: cancellationToken);
            _lastRenewalTime = DateTime.UtcNow;
        }
        catch
        {
            throw new InvalidOperationException("Failed to renew stream lock. Operation aborted to prevent data corruption.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            try
            {
                await LeaseClient.ReleaseAsync();
            }
            catch
            {
            }
            _disposed = true;
        }
    }
}