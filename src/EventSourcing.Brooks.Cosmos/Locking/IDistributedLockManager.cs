using System;
using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.EventSourcing.Brooks.Cosmos.Locking;

/// <summary>
///     Provides functionality for acquiring and managing distributed locks.
/// </summary>
internal interface IDistributedLockManager
{
    /// <summary>
    ///     Acquires a distributed lock with the specified key and duration.
    /// </summary>
    /// <param name="lockKey">The unique key identifying the resource to lock.</param>
    /// <param name="duration">The duration for which to hold the lock.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A distributed lock instance if successfully acquired.</returns>
    Task<IDistributedLock> AcquireLockAsync(
        string lockKey,
        TimeSpan duration,
        CancellationToken cancellationToken = default
    );
}