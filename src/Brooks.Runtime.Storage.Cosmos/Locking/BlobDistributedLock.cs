using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Azure;


namespace Mississippi.Brooks.Runtime.Storage.Cosmos.Locking;

/// <summary>
///     Distributed lock implementation using Azure Blob Storage leases.
/// </summary>
internal sealed class BlobDistributedLock : IDistributedLock
{
    private readonly Stopwatch heldDurationStopwatch;

    private readonly string lockKey;

    private readonly TimeSpan renewalThreshold;

    private bool disposed;

    private long lastRenewalTimestamp;

    /// <summary>
    ///     Initializes a new instance of the <see cref="BlobDistributedLock" /> class.
    /// </summary>
    /// <param name="leaseClient">The blob lease client for managing the lock.</param>
    /// <param name="lockId">The unique identifier for the lock.</param>
    /// <param name="leaseRenewalThresholdSeconds">The threshold in seconds for lease renewal.</param>
    /// <param name="leaseDurationSeconds">The duration in seconds for the lease.</param>
    /// <param name="lockKey">The lock key for metrics reporting.</param>
    /// <param name="heldDurationStopwatch">Stopwatch started when lock was acquired, for measuring held duration.</param>
    /// <param name="timeProvider">Time provider for timestamps. If null, uses <see cref="TimeProvider.System" />.</param>
    public BlobDistributedLock(
        IBlobLeaseClient leaseClient,
        string lockId,
        int leaseRenewalThresholdSeconds,
        int leaseDurationSeconds,
        string lockKey,
        Stopwatch heldDurationStopwatch,
        TimeProvider? timeProvider = null
    )
    {
        LeaseClient = leaseClient;
        LockId = lockId;
        LeaseRenewalThresholdSeconds = leaseRenewalThresholdSeconds;
        LeaseDurationSeconds = leaseDurationSeconds;
        this.lockKey = lockKey;
        this.heldDurationStopwatch = heldDurationStopwatch;
        TimeProvider = timeProvider ?? TimeProvider.System;
        lastRenewalTimestamp = TimeProvider.GetTimestamp();

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

    private TimeProvider TimeProvider { get; }

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
    public Task RenewAsync(
        CancellationToken cancellationToken = default
    ) =>
        RenewAsync(false, cancellationToken);

    /// <summary>
    ///     Renews the lease through the service when forced or when the ordinary threshold is reached.
    /// </summary>
    /// <param name="forceRenewal">Whether an actual service renewal is required.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the actual renewal or ordinary early skip.</returns>
    public async Task RenewAsync(
        bool forceRenewal,
        CancellationToken cancellationToken = default
    )
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        long requestStart = TimeProvider.GetTimestamp();
        TimeSpan timeSinceLastRenewal = TimeProvider.GetElapsedTime(lastRenewalTimestamp, requestStart);

        // Only renew if we're approaching the expiration threshold
        if (!forceRenewal && (timeSinceLastRenewal < renewalThreshold))
        {
            return;
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await LeaseClient.RenewAsync(cancellationToken: cancellationToken);

            // The service may have renewed before the response reaches this process.
            lastRenewalTimestamp = requestStart;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
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