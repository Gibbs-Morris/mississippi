using System.Net;
using Microsoft.Azure.Cosmos;

namespace Mississippi.EventSourcing.Cosmos.Retry;

internal class CosmosRetryPolicy : IRetryPolicy
{
    private int MaxRetries { get; }

    public CosmosRetryPolicy(int maxRetries = 3)
    {
        MaxRetries = maxRetries;
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        Exception? lastException = null;

        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests && attempt < MaxRetries)
            {
                lastException = ex;
                var delay = ex.RetryAfter ?? TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100);
                await Task.Delay(delay, cancellationToken);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.RequestEntityTooLarge)
            {
                throw new InvalidOperationException("Request size exceeds maximum allowed limit. Consider reducing batch size.", ex);
            }
            catch (Exception ex) when (attempt < MaxRetries)
            {
                lastException = ex;
                var delay = TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100);
                await Task.Delay(delay, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Operation failed after {MaxRetries + 1} attempts", ex);
            }
        }

        throw new InvalidOperationException($"Operation failed after {MaxRetries + 1} attempts", lastException);
    }
}