namespace Mississippi.EventSourcing.Cosmos.Locking;

internal interface IDistributedLock : IAsyncDisposable
{
    string LockId { get; }
    Task RenewAsync(CancellationToken cancellationToken = default);
}