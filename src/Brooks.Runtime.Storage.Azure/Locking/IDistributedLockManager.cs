using System;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.Brooks.Abstractions;


namespace Mississippi.Brooks.Runtime.Storage.Azure.Locking;

/// <summary>
///     Acquires distributed locks for Brooks Azure stream operations.
/// </summary>
internal interface IDistributedLockManager
{
    /// <summary>
    ///     Acquires a distributed lock for the specified brook.
    /// </summary>
    /// <param name="brookId">The brook identifier.</param>
    /// <param name="duration">The desired lease duration.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous acquire operation.</returns>
    Task<IDistributedLock> AcquireLockAsync(
        BrookKey brookId,
        TimeSpan duration,
        CancellationToken cancellationToken = default
    );
}