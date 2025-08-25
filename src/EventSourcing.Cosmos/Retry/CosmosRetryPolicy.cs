﻿using System.Net;

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
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.RequestEntityTooLarge)
            {
                throw new InvalidOperationException(
                    "Request size exceeds maximum allowed limit. Consider reducing batch size.",
                    ex);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                throw;
            }
            catch (CosmosException ex) when (IsTransientCosmosStatus(ex.StatusCode) && (attempt < MaxRetries))
            {
                lastException = ex;
                TimeSpan delay = ComputeDelay(ex, attempt);
                await Task.Delay(delay, cancellationToken);
            }
            catch (CosmosException ex)
            {
                // Non-transient Cosmos error - wrap with context and rethrow
                throw new InvalidOperationException($"Cosmos operation failed with status {ex.StatusCode}", ex);
            }
            catch (TaskCanceledException ex) when (attempt < MaxRetries)
            {
                lastException = ex;
                TimeSpan delay = TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100);
                await Task.Delay(delay, cancellationToken);
            }
            catch (TaskCanceledException ex)
            {
                // Cancellation should be honored by callers; wrap to provide consistent surface
                throw new OperationCanceledException("Cosmos operation canceled", ex, cancellationToken);
            }
        }

        throw new InvalidOperationException($"Operation failed after {MaxRetries + 1} attempts", lastException);
    }

    private static bool IsTransientCosmosStatus(
        HttpStatusCode? statusCode
    ) =>
        (statusCode == HttpStatusCode.TooManyRequests) ||
        (statusCode == HttpStatusCode.ServiceUnavailable) ||
        (statusCode == HttpStatusCode.RequestTimeout) ||
        (statusCode == HttpStatusCode.InternalServerError) ||
        (statusCode == HttpStatusCode.GatewayTimeout);

    private static TimeSpan ComputeDelay(
        CosmosException ex,
        int attempt
    )
    {
        if (ex.RetryAfter.HasValue)
        {
            return ex.RetryAfter.Value;
        }

        // Exponential backoff base 100ms
        return TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100);
    }
}