namespace Mississippi.EventSourcing.Cosmos.Locking;

internal interface IDistributedLockManager
{
    Task<IDistributedLock> AcquireLockAsync(string lockKey, TimeSpan duration, CancellationToken cancellationToken = default);
}