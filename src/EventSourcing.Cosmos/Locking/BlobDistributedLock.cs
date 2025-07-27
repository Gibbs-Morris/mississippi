using Azure.Storage.Blobs.Specialized;

namespace Mississippi.EventSourcing.Cosmos.Locking;

/// <summary>
/// Distributed lock implementation using Azure Blob Storage leases.
/// </summary>
internal sealed class BlobDistributedLock : IDistributedLock
{
    private DateTime lastRenewalTime;

    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobDistributedLock"/> class.
    /// </summary>
    /// <param name="leaseClient">The blob lease client for managing the lock.</param>
    /// <param name="lockId">The unique identifier for the lock.</param>
    /// <param name="leaseRenewalThresholdSeconds">The threshold in seconds for lease renewal.</param>
    /// <param name="leaseDurationSeconds">The duration in seconds for the lease.</param>
    public BlobDistributedLock(BlobLeaseClient leaseClient, string lockId, int leaseRenewalThresholdSeconds, int leaseDurationSeconds)
    {
        LeaseClient = leaseClient;
        LockId = lockId;
        LeaseRenewalThresholdSeconds = leaseRenewalThresholdSeconds;
        LeaseDurationSeconds = leaseDurationSeconds;
        lastRenewalTime = DateTime.UtcNow;
    }

    private BlobLeaseClient LeaseClient { get; }

    private int LeaseRenewalThresholdSeconds { get; }

    private int LeaseDurationSeconds { get; }

    /// <summary>
    /// Gets the unique identifier for the lock.
    /// </summary>
    public string LockId { get; }

    /// <summary>
    /// Renews the distributed lock lease.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the lock has been disposed.</exception>
    public async Task RenewAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        var timeSinceLastRenewal = DateTime.UtcNow - lastRenewalTime;
        var renewalThreshold = TimeSpan.FromSeconds(LeaseDurationSeconds - LeaseRenewalThresholdSeconds);

        if (timeSinceLastRenewal < renewalThreshold)
        {
            return;
        }

        try
        {
            await LeaseClient.RenewAsync(cancellationToken: cancellationToken);
            lastRenewalTime = DateTime.UtcNow;
        }
        catch
        {
            throw new InvalidOperationException("Failed to renew stream lock. Operation aborted to prevent data corruption.");
        }
    }

    /// <summary>
    /// Asynchronously disposes the distributed lock and releases the lease.
    /// </summary>
    /// <returns>A task representing the asynchronous disposal operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (!disposed)
        {
            try
            {
                await LeaseClient.ReleaseAsync();
            }
            catch (Azure.RequestFailedException)
            {
                // Ignore request failures during disposal (e.g., blob not found, lease already released)
            }
            catch (InvalidOperationException)
            {
                // Ignore invalid operation exceptions during disposal (e.g., lease already released)
            }

            disposed = true;
        }
    }
}