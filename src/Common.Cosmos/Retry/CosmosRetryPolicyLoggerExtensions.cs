using System;

using Microsoft.Extensions.Logging;


namespace Mississippi.Common.Cosmos.Retry;

/// <summary>
///     High-performance logging extensions for <see cref="CosmosRetryPolicy" />.
/// </summary>
internal static partial class CosmosRetryPolicyLoggerExtensions
{
    /// <summary>
    ///     Logs when all retry attempts have been exhausted.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="maxRetries">The maximum number of retries attempted.</param>
    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Error,
        Message = "Cosmos operation failed after {MaxRetries} retry attempts")]
    public static partial void AllRetriesExhausted(
        this ILogger logger,
        int maxRetries
    );

    /// <summary>
    ///     Logs when a Cosmos operation fails with a non-transient error.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception that was thrown.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Error,
        Message = "Cosmos operation failed with non-transient error (status={StatusCode})")]
    public static partial void NonTransientError(
        this ILogger logger,
        Exception exception,
        int statusCode
    );

    /// <summary>
    ///     Logs when a Cosmos operation is cancelled.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The cancellation exception.</param>
    /// <param name="wasCancellationRequested">Whether cancellation was explicitly requested.</param>
    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Information,
        Message = "Cosmos operation cancelled (requested={WasCancellationRequested})")]
    public static partial void OperationCancelled(
        this ILogger logger,
        Exception exception,
        bool wasCancellationRequested
    );

    /// <summary>
    ///     Logs when a Cosmos operation completes successfully.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="attempt">The attempt number on which it succeeded (0-based).</param>
    [LoggerMessage(EventId = 5, Level = LogLevel.Debug, Message = "Cosmos operation succeeded on attempt {Attempt}")]
    public static partial void OperationSucceeded(
        this ILogger logger,
        int attempt
    );

    /// <summary>
    ///     Logs when a retry attempt is being made after a transient failure.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="attempt">The current attempt number (0-based).</param>
    /// <param name="statusCode">The HTTP status code that triggered the retry.</param>
    /// <param name="delayMs">The delay in milliseconds before the next attempt.</param>
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Cosmos transient error (status={StatusCode}), retrying attempt {Attempt} after {DelayMs}ms")]
    public static partial void RetryingAfterTransientError(
        this ILogger logger,
        int attempt,
        int statusCode,
        double delayMs
    );

    /// <summary>
    ///     Logs when starting a Cosmos operation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(EventId = 6, Level = LogLevel.Debug, Message = "Starting Cosmos operation with retry policy")]
    public static partial void StartingOperation(
        this ILogger logger
    );
}