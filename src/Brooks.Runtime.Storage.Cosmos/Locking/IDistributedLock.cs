using System;
using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.Brooks.Runtime.Storage.Cosmos.Locking;

/// <summary>
///     Represents a distributed lock for coordinating access to shared resources across multiple instances.
/// </summary>
internal interface IDistributedLock : IAsyncDisposable
{
    /// <summary>
    ///     Gets the unique identifier for this lock.
    /// </summary>
    string LockId { get; }

    /// <summary>
    ///     Renews the lock to extend its lease duration.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous renewal operation.</returns>
    Task RenewAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Renews the lock, optionally requiring an actual service request before the normal threshold.
    /// </summary>
    /// <param name="forceRenewal">Whether to bypass the young-lease renewal skip.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous renewal operation.</returns>
    Task RenewAsync(
        bool forceRenewal,
        CancellationToken cancellationToken = default
    );
}