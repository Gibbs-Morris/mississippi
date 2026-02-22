using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

using Mississippi.Common.Cosmos.Abstractions.Retry;


namespace Mississippi.Common.Cosmos.Retry;

/// <summary>
///     Retry policy implementation for Cosmos DB operations.
/// </summary>
public sealed class CosmosRetryPolicy : IRetryPolicy
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CosmosRetryPolicy" /> class with a configurable retry count.
    /// </summary>
    /// <param name="logger">Logger instance for logging retry operations.</param>
    /// <param name="maxRetries">Maximum number of retry attempts for transient failures.</param>
    public CosmosRetryPolicy(
        ILogger<CosmosRetryPolicy> logger,
        int maxRetries = 3
    )
    {
        ArgumentNullException.ThrowIfNull(logger);
        MaxRetries = maxRetries;
        Logger = logger;
    }

    private ILogger<CosmosRetryPolicy> Logger { get; }

    private int MaxRetries { get; }

    private static bool IsTransient(
        HttpStatusCode? statusCode
    ) =>
        (statusCode == HttpStatusCode.TooManyRequests) ||
        (statusCode == HttpStatusCode.ServiceUnavailable) ||
        (statusCode == HttpStatusCode.RequestTimeout) ||
        (statusCode == HttpStatusCode.InternalServerError) ||
        (statusCode == HttpStatusCode.GatewayTimeout);

    private static void ThrowKnownCosmosErrors(
        CosmosException exception
    )
    {
        switch (exception.StatusCode)
        {
            case HttpStatusCode.RequestEntityTooLarge:
                throw new InvalidOperationException(
                    "Request size exceeds maximum allowed limit. Consider reducing payload size.",
                    exception);
            case HttpStatusCode.NotFound:
                throw exception;
        }
    }

    /// <inheritdoc />
    public async Task<T> ExecuteAsync<T>(
        Func<Task<T>> operation,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(operation);
        Logger.StartingOperation();
        Exception? lastException = null;
        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                T result = await operation().ConfigureAwait(false);
                Logger.OperationSucceeded(attempt);
                return result;
            }
            catch (CosmosException ex)
            {
                ThrowKnownCosmosErrors(ex);
                if (IsTransient(ex.StatusCode))
                {
                    lastException = ex;
                    if (await TryDelayForRetryAsync(ex, attempt, cancellationToken).ConfigureAwait(false))
                    {
                        continue;
                    }
                }

                Logger.NonTransientError(ex, (int)ex.StatusCode);
                throw new InvalidOperationException($"Cosmos operation failed with status {ex.StatusCode}", ex);
            }
            catch (TaskCanceledException ex)
            {
                Logger.OperationCancelled(ex, cancellationToken.IsCancellationRequested);
                throw new OperationCanceledException(
                    cancellationToken.IsCancellationRequested
                        ? "Cosmos operation canceled"
                        : "Cosmos operation canceled before completion.",
                    ex,
                    cancellationToken);
            }
        }

        Logger.AllRetriesExhausted(MaxRetries);
        throw new InvalidOperationException($"Operation failed after {MaxRetries + 1} attempts", lastException);
    }

    private async Task<bool> TryDelayForRetryAsync(
        CosmosException exception,
        int attempt,
        CancellationToken cancellationToken
    )
    {
        if (attempt >= MaxRetries)
        {
            return false;
        }

        TimeSpan delay = exception.RetryAfter ?? TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100);
        Logger.RetryingAfterTransientError(attempt, (int)exception.StatusCode, delay.TotalMilliseconds);
        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        return true;
    }
}