using Azure;


namespace Mississippi.EventSourcing.Cosmos.Locking;

/// <summary>
///     Distributed lock implementation using Azure Blob Storage leases.
/// </summary>
internal sealed class BlobDistributedLock : IDistributedLock
{
    private readonly TimeSpan renewalThreshold;

    private bool disposed;

    private DateTimeOffset lastRenewalTime;

    /// <summary>
    ///     Initializes a new instance of the <see cref="BlobDistributedLock" /> class.
    /// </summary>
    /// <param name="leaseClient">The blob lease client for managing the lock.</param>
    /// <param name="lockId">The unique identifier for the lock.</param>
    /// <param name="leaseRenewalThresholdSeconds">The threshold in seconds for lease renewal.</param>
    /// <param name="leaseDurationSeconds">The duration in seconds for the lease.</param>
    public BlobDistributedLock(
        IBlobLeaseClient leaseClient,
        string lockId,
        int leaseRenewalThresholdSeconds,
        int leaseDurationSeconds
    )
    {
        LeaseClient = leaseClient;
        LockId = lockId;
        LeaseRenewalThresholdSeconds = leaseRenewalThresholdSeconds;
        LeaseDurationSeconds = leaseDurationSeconds;
        lastRenewalTime = DateTimeOffset.UtcNow;

        // Calculate renewal threshold with a safety buffer to account for network latency
        renewalThreshold = TimeSpan.FromSeconds(Math.Max(1, leaseDurationSeconds - leaseRenewalThresholdSeconds - 1));
    }

    private IBlobLeaseClient LeaseClient { get; }

    private int LeaseRenewalThresholdSeconds { get; }

    private int LeaseDurationSeconds { get; }

    /// <summary>
    ///     Gets the unique identifier for the lock.
    /// </summary>
    public string LockId { get; }

    /// <summary>
    ///     Renews the distributed lock lease.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the lock has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the lock cannot be renewed.</exception>
    public async Task RenewAsync(
        CancellationToken cancellationToken = default
    )
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        TimeSpan timeSinceLastRenewal = now - lastRenewalTime;

        // Only renew if we're approaching the expiration threshold
        if (timeSinceLastRenewal < renewalThreshold)
        {
            return;
        }

        try
        {
            await LeaseClient.RenewAsync(cancellationToken: cancellationToken);

            // Use the actual completion time to avoid drifting too close to expiration
            lastRenewalTime = DateTimeOffset.UtcNow;
        }
        catch (RequestFailedException ex) when ((ex.Status == 409) || (ex.Status == 404))
        {
            // Lease lost or blob not found - this is a critical failure
            throw new InvalidOperationException(
                "Failed to renew brook lock - lease has been lost. Operation aborted to prevent data corruption.",
                ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Failed to renew brook lock. Operation aborted to prevent data corruption.",
                ex);
        }
    }

    /// <summary>
    ///     Asynchronously disposes the distributed lock and releases the lease.
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
            catch (RequestFailedException)
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
