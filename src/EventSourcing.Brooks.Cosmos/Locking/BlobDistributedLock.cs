using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Azure;


namespace Mississippi.EventSourcing.Brooks.Cosmos.Locking;

/// <summary>
///     Distributed lock implementation using Azure Blob Storage leases.
/// </summary>
internal sealed class BlobDistributedLock : IDistributedLock
{
    private readonly Stopwatch heldDurationStopwatch;

    private readonly string lockKey;

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
    /// <param name="lockKey">The lock key for metrics reporting.</param>
    /// <param name="heldDurationStopwatch">Stopwatch started when lock was acquired, for measuring held duration.</param>
    public BlobDistributedLock(
        IBlobLeaseClient leaseClient,
        string lockId,
        int leaseRenewalThresholdSeconds,
        int leaseDurationSeconds,
        string lockKey,
        Stopwatch heldDurationStopwatch
    )
    {
        LeaseClient = leaseClient;
        LockId = lockId;
        LeaseRenewalThresholdSeconds = leaseRenewalThresholdSeconds;
        LeaseDurationSeconds = leaseDurationSeconds;
        this.lockKey = lockKey;
        this.heldDurationStopwatch = heldDurationStopwatch;
        lastRenewalTime = DateTimeOffset.UtcNow;

        // Calculate renewal threshold with a safety buffer to account for network latency
        renewalThreshold = TimeSpan.FromSeconds(Math.Max(1, leaseDurationSeconds - leaseRenewalThresholdSeconds - 1));
    }

    /// <summary>
    ///     Gets the unique identifier for the lock.
    /// </summary>
    public string LockId { get; }

    private IBlobLeaseClient LeaseClient { get; }

    private int LeaseDurationSeconds { get; }

    private int LeaseRenewalThresholdSeconds { get; }

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
            finally
            {
                // Record how long the lock was held
                heldDurationStopwatch.Stop();
                LockMetrics.RecordHeldDuration(lockKey, heldDurationStopwatch.Elapsed.TotalMilliseconds);
            }

            disposed = true;
        }
    }

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
}