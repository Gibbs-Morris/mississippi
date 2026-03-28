using System;
using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.Brooks.Runtime.Storage.Azure.Locking;

/// <summary>
///     Represents a distributed lock held for Brooks Azure append or recovery coordination.
/// </summary>
internal interface IDistributedLock : IAsyncDisposable
{
    /// <summary>
    ///     Gets the storage-provider lock identifier.
    /// </summary>
    string LockId { get; }

    /// <summary>
    ///     Renews the distributed lock when the current lease is nearing expiry.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous renewal operation.</returns>
    Task RenewAsync(
        CancellationToken cancellationToken = default
    );
}