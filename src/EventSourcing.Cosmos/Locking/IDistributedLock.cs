namespace Mississippi.EventSourcing.Cosmos.Locking;

/// <summary>
/// Represents a distributed lock for coordinating access to shared resources across multiple instances.
/// </summary>
internal interface IDistributedLock : IAsyncDisposable
{
    /// <summary>
    /// Gets the unique identifier for this lock.
    /// </summary>
    string LockId { get; }

    /// <summary>
    /// Renews the lock to extend its lease duration.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous renewal operation.</returns>
    Task RenewAsync(CancellationToken cancellationToken = default);
}