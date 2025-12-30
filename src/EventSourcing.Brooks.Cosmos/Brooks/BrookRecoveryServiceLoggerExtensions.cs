using System;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Brooks.Abstractions;


namespace Mississippi.EventSourcing.Brooks.Cosmos.Brooks;

/// <summary>
///     High-performance logging extensions for <see cref="BrookRecoveryService" />.
/// </summary>
internal static partial class BrookRecoveryServiceLoggerExtensions
{
    /// <summary>
    ///     Logs when acquiring recovery lock.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="brookId">The brook identifier.</param>
    /// <param name="timeoutSeconds">The timeout in seconds.</param>
    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Debug,
        Message = "Acquiring recovery lock for brook '{BrookId}' with {TimeoutSeconds}s timeout")]
    public static partial void AcquiringRecoveryLock(
        this ILogger logger,
        BrookKey brookId,
        double timeoutSeconds
    );

    /// <summary>
    ///     Logs when checking if all events exist for recovery decision.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="brookId">The brook identifier.</param>
    /// <param name="originalPosition">The original position.</param>
    /// <param name="targetPosition">The target position.</param>
    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Debug,
        Message =
            "Checking if all events exist for brook '{BrookId}' between positions {OriginalPosition} and {TargetPosition}")]
    public static partial void CheckingEventsExist(
        this ILogger logger,
        BrookKey brookId,
        long originalPosition,
        long targetPosition
    );

    /// <summary>
    ///     Logs when cursor position is returned.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="brookId">The brook identifier.</param>
    /// <param name="position">The cursor position.</param>
    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "Cursor position for brook '{BrookId}' is {Position}")]
    public static partial void CursorPositionReturned(
        this ILogger logger,
        BrookKey brookId,
        long position
    );

    /// <summary>
    ///     Logs when deleting an orphaned event during rollback.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="brookId">The brook identifier.</param>
    /// <param name="position">The position of the event being deleted.</param>
    [LoggerMessage(
        EventId = 9,
        Level = LogLevel.Debug,
        Message = "Deleting orphaned event at position {Position} for brook '{BrookId}'")]
    public static partial void DeletingOrphanedEvent(
        this ILogger logger,
        BrookKey brookId,
        long position
    );

    /// <summary>
    ///     Logs when starting to get or recover cursor position.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="brookId">The brook identifier.</param>
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Getting or recovering cursor position for brook '{BrookId}'")]
    public static partial void GettingOrRecoveringCursor(
        this ILogger logger,
        BrookKey brookId
    );

    /// <summary>
    ///     Logs when a pending cursor is detected and recovery is starting.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="brookId">The brook identifier.</param>
    /// <param name="originalPosition">The original position before the orphaned operation.</param>
    /// <param name="targetPosition">The target position of the orphaned operation.</param>
    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Warning,
        Message =
            "Pending cursor detected for brook '{BrookId}', initiating recovery (original: {OriginalPosition}, target: {TargetPosition})")]
    public static partial void PendingCursorDetected(
        this ILogger logger,
        BrookKey brookId,
        long originalPosition,
        long targetPosition
    );

    /// <summary>
    ///     Logs when committing cursor after successful recovery.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="brookId">The brook identifier.</param>
    /// <param name="targetPosition">The target position being committed.</param>
    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Information,
        Message = "Recovery successful for brook '{BrookId}', committing cursor to position {TargetPosition}")]
    public static partial void RecoveryCommitting(
        this ILogger logger,
        BrookKey brookId,
        long targetPosition
    );

    /// <summary>
    ///     Logs when recovery fails with an exception.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="brookId">The brook identifier.</param>
    [LoggerMessage(EventId = 11, Level = LogLevel.Error, Message = "Recovery failed for brook '{BrookId}'")]
    public static partial void RecoveryFailed(
        this ILogger logger,
        Exception exception,
        BrookKey brookId
    );

    /// <summary>
    ///     Logs when recovery lock acquisition failed.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="brookId">The brook identifier.</param>
    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Information,
        Message = "Could not acquire recovery lock for brook '{BrookId}', another process may be handling recovery")]
    public static partial void RecoveryLockFailed(
        this ILogger logger,
        BrookKey brookId
    );

    /// <summary>
    ///     Logs when recovery lock acquisition failed with exception details.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="brookId">The brook identifier.</param>
    [LoggerMessage(
        EventId = 12,
        Level = LogLevel.Information,
        Message = "Could not acquire recovery lock for brook '{BrookId}', another process may be handling recovery")]
    public static partial void RecoveryLockFailed(
        this ILogger logger,
        Exception exception,
        BrookKey brookId
    );

    /// <summary>
    ///     Logs when rollback completes successfully.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="brookId">The brook identifier.</param>
    /// <param name="eventsDeleted">The number of events deleted.</param>
    [LoggerMessage(
        EventId = 10,
        Level = LogLevel.Information,
        Message = "Rollback completed for brook '{BrookId}', deleted {EventsDeleted} orphaned events")]
    public static partial void RollbackCompleted(
        this ILogger logger,
        BrookKey brookId,
        long eventsDeleted
    );

    /// <summary>
    ///     Logs when rolling back an orphaned operation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="brookId">The brook identifier.</param>
    /// <param name="originalPosition">The original position to roll back to.</param>
    /// <param name="targetPosition">The target position from which to roll back.</param>
    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Warning,
        Message =
            "Rolling back orphaned operation for brook '{BrookId}' from position {TargetPosition} to {OriginalPosition}")]
    public static partial void RollingBack(
        this ILogger logger,
        BrookKey brookId,
        long originalPosition,
        long targetPosition
    );
}