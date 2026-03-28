using System;
using System.Threading;
using System.Threading.Tasks;

using Azure;
using Azure.Storage.Blobs.Specialized;


namespace Mississippi.Brooks.Runtime.Storage.Azure.Locking;

/// <summary>
///     Wraps an Azure Blob lease as a Brooks distributed lock instance.
/// </summary>
internal sealed class BlobDistributedLock : IDistributedLock
{
    private readonly TimeSpan renewalThreshold;

    private readonly TimeProvider timeProvider;

    private bool disposed;

    private DateTimeOffset lastRenewal;

    /// <summary>
    ///     Initializes a new instance of the <see cref="BlobDistributedLock" /> class.
    /// </summary>
    /// <param name="leaseClient">The Azure blob lease client.</param>
    /// <param name="lockId">The acquired lease identifier.</param>
    /// <param name="leaseDurationSeconds">The configured lease duration in seconds.</param>
    /// <param name="leaseRenewalThresholdSeconds">The renewal threshold in seconds.</param>
    /// <param name="timeProvider">The time provider used for renewal decisions.</param>
    public BlobDistributedLock(
        BlobLeaseClient leaseClient,
        string lockId,
        int leaseDurationSeconds,
        int leaseRenewalThresholdSeconds,
        TimeProvider? timeProvider = null
    )
    {
        LeaseClient = leaseClient ?? throw new ArgumentNullException(nameof(leaseClient));
        LockId = string.IsNullOrWhiteSpace(lockId) ? throw new ArgumentException("Lock ID must not be empty.", nameof(lockId)) : lockId;
        this.timeProvider = timeProvider ?? TimeProvider.System;
        lastRenewal = this.timeProvider.GetUtcNow();
        renewalThreshold = TimeSpan.FromSeconds(Math.Max(1, leaseDurationSeconds - leaseRenewalThresholdSeconds));
    }

    /// <inheritdoc />
    public string LockId { get; }

    private BlobLeaseClient LeaseClient { get; }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (disposed)
        {
            return;
        }

        try
        {
            await LeaseClient.ReleaseAsync().ConfigureAwait(false);
        }
        catch (RequestFailedException exception)
        {
            if ((exception.Status != 404) && (exception.Status != 409) && (exception.Status != 412))
            {
                throw;
            }
        }
        finally
        {
            disposed = true;
        }
    }

    /// <inheritdoc />
    public async Task RenewAsync(
        CancellationToken cancellationToken = default
    )
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if ((timeProvider.GetUtcNow() - lastRenewal) < renewalThreshold)
        {
            return;
        }

        try
        {
            await LeaseClient.RenewAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            lastRenewal = timeProvider.GetUtcNow();
        }
        catch (RequestFailedException exception) when ((exception.Status == 404) || (exception.Status == 409) || (exception.Status == 412))
        {
            throw new BrookStorageRetryableException(
                "Brooks Azure write fencing lease was lost before the append completed. Re-read committed state before retrying.",
                exception);
        }
    }
}