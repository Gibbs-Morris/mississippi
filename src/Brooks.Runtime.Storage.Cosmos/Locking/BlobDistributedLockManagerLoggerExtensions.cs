using Microsoft.Extensions.Logging;


namespace Mississippi.EventSourcing.Brooks.Cosmos.Locking;

/// <summary>
///     High-performance logging extensions for <see cref="BlobDistributedLockManager" />.
/// </summary>
internal static partial class BlobDistributedLockManagerLoggerExtensions
{
    /// <summary>
    ///     Logs when attempting to acquire a distributed lock.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="lockKey">The lock key.</param>
    /// <param name="durationSeconds">The lock duration in seconds.</param>
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Acquiring distributed lock for key '{LockKey}' with duration {DurationSeconds}s")]
    public static partial void AcquiringLock(
        this ILogger logger,
        string lockKey,
        double durationSeconds
    );

    /// <summary>
    ///     Logs when creating a new lock blob.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="lockKey">The lock key.</param>
    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "Creating lock blob for key '{LockKey}'")]
    public static partial void CreatingLockBlob(
        this ILogger logger,
        string lockKey
    );

    /// <summary>
    ///     Logs when a lock is successfully acquired.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="lockKey">The lock key.</param>
    /// <param name="leaseId">The lease ID.</param>
    /// <param name="attempt">The attempt number (0-based).</param>
    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "Acquired distributed lock for key '{LockKey}' with lease '{LeaseId}' on attempt {Attempt}")]
    public static partial void LockAcquired(
        this ILogger logger,
        string lockKey,
        string leaseId,
        int attempt
    );

    /// <summary>
    ///     Logs when lock acquisition fails after all retries.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="lockKey">The lock key.</param>
    /// <param name="maxAttempts">The maximum number of attempts made.</param>
    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Warning,
        Message = "Failed to acquire distributed lock for key '{LockKey}' after {MaxAttempts} attempts")]
    public static partial void LockAcquisitionFailed(
        this ILogger logger,
        string lockKey,
        int maxAttempts
    );

    /// <summary>
    ///     Logs when a lock acquisition attempt fails due to conflict.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="lockKey">The lock key.</param>
    /// <param name="attempt">The attempt number (0-based).</param>
    /// <param name="backoffMs">The backoff time in milliseconds.</param>
    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Debug,
        Message = "Lock conflict for key '{LockKey}' on attempt {Attempt}, backing off {BackoffMs}ms")]
    public static partial void LockConflict(
        this ILogger logger,
        string lockKey,
        int attempt,
        int backoffMs
    );
}