using System.Net;

using Microsoft.Azure.Cosmos;


namespace Mississippi.EventSourcing.Cosmos.Retry;

/// <summary>
///     Retry policy implementation for Cosmos DB operations.
/// </summary>
internal class CosmosRetryPolicy : IRetryPolicy
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CosmosRetryPolicy" /> class.
    /// </summary>
    /// <param name="maxRetries">The maximum number of retry attempts.</param>
    public CosmosRetryPolicy(
        int maxRetries = 3
    ) =>
        MaxRetries = maxRetries;

    private int MaxRetries { get; }

    /// <summary>
    ///     Executes an operation with retry logic for transient failures.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="Exception">Thrown when all retry attempts are exhausted.</exception>
    public async Task<T> ExecuteAsync<T>(
        Func<Task<T>> operation,
        CancellationToken cancellationToken = default
    )
    {
        Exception? lastException = null;
        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (CosmosException ex) when
                ((ex.StatusCode == HttpStatusCode.TooManyRequests) && (attempt < MaxRetries))
            {
                lastException = ex;
                TimeSpan delay = ex.RetryAfter ?? TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100);
                await Task.Delay(delay, cancellationToken);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.RequestEntityTooLarge)
            {
                throw new InvalidOperationException(
                    "Request size exceeds maximum allowed limit. Consider reducing batch size.",
                    ex);
            }
            // Pass through NotFound so callers which expect it (e.g., probing for a missing item) can handle it
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                throw;
            }
            // Retry only on transient Cosmos statuses; allow NotFound and other non-transient errors to bubble up
            catch (CosmosException ex) when (
                attempt < MaxRetries &&
                (ex.StatusCode == HttpStatusCode.ServiceUnavailable ||
                 ex.StatusCode == HttpStatusCode.RequestTimeout ||
                 ex.StatusCode == HttpStatusCode.InternalServerError ||
                 ex.StatusCode == HttpStatusCode.GatewayTimeout))
            {
                lastException = ex;
                TimeSpan delay = TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100);
                await Task.Delay(delay, cancellationToken);
            }
            catch (TaskCanceledException ex) when (attempt < MaxRetries)
            {
                lastException = ex;
                TimeSpan delay = TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100);
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