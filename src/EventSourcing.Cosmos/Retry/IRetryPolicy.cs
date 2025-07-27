namespace Mississippi.EventSourcing.Cosmos.Retry;

internal interface IRetryPolicy
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);
}