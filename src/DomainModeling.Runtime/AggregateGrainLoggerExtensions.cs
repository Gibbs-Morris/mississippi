using System;

using Microsoft.Extensions.Logging;

using Mississippi.DomainModeling.Abstractions;


namespace Mississippi.DomainModeling.Runtime;

/// <summary>
///     High-performance logging extensions for aggregate grains.
/// </summary>
internal static partial class AggregateGrainLoggerExtensions
{
    /// <summary>
    ///     Logs when the aggregate grain is activated.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="aggregateKey">The aggregate key.</param>
    [LoggerMessage(0, LogLevel.Debug, "Aggregate grain activated: {AggregateKey}")]
    public static partial void Activated(
        this ILogger logger,
        string aggregateKey
    );

    /// <summary>
    ///     Logs when a command has been successfully executed.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="commandType">The type of command executed.</param>
    /// <param name="aggregateKey">The aggregate key.</param>
    [LoggerMessage(2, LogLevel.Debug, "Executed command {CommandType} for aggregate {AggregateKey}")]
    public static partial void CommandExecuted(
        this ILogger logger,
        string commandType,
        string aggregateKey
    );

    /// <summary>
    ///     Logs when a command execution fails.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="commandType">The type of command that failed.</param>
    /// <param name="aggregateKey">The aggregate key.</param>
    /// <param name="errorCode">The error code.</param>
    /// <param name="errorMessage">The error message.</param>
    [LoggerMessage(
        3,
        LogLevel.Warning,
        "Command {CommandType} failed for aggregate {AggregateKey}: {ErrorCode} - {ErrorMessage}")]
    public static partial void CommandFailed(
        this ILogger logger,
        string commandType,
        string aggregateKey,
        string errorCode,
        string errorMessage
    );

    /// <summary>
    ///     Logs when a command is received for processing.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="commandType">The type of command received.</param>
    /// <param name="aggregateKey">The aggregate key.</param>
    [LoggerMessage(1, LogLevel.Debug, "Received command {CommandType} for aggregate {AggregateKey}")]
    public static partial void CommandReceived(
        this ILogger logger,
        string commandType,
        string aggregateKey
    );

    /// <summary>
    ///     Logs when effect dispatch fails after command events are persisted.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="commandType">The type of command that was executed.</param>
    /// <param name="aggregateKey">The aggregate key.</param>
    /// <param name="exception">The exception that occurred during effect dispatch.</param>
    [LoggerMessage(
        10,
        LogLevel.Error,
        "Effect dispatch failed for command {CommandType} on aggregate {AggregateKey}. " +
        "Command events were persisted successfully but effect processing encountered an error.")]
    public static partial void EffectDispatchFailed(
        this ILogger logger,
        string commandType,
        string aggregateKey,
        Exception exception
    );

    /// <summary>
    ///     Logs when aggregate state hydration begins.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="aggregateKey">The aggregate key.</param>
    /// <param name="position">The position to replay from.</param>
    [LoggerMessage(
        4,
        LogLevel.Debug,
        "Hydrating state for aggregate {AggregateKey}, replaying from position {Position}")]
    public static partial void HydratingState(
        this ILogger logger,
        string aggregateKey,
        long position
    );

    /// <summary>
    ///     Logs when no snapshot is available and state will be built from scratch.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="aggregateKey">The aggregate key.</param>
    [LoggerMessage(5, LogLevel.Debug, "No snapshot available for aggregate {AggregateKey}, building from scratch")]
    public static partial void NoSnapshotAvailable(
        this ILogger logger,
        string aggregateKey
    );

    /// <summary>
    ///     Logs when event reducer hash does not match the snapshot, requiring a rebuild.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="aggregateKey">The aggregate key.</param>
    /// <param name="snapshotHash">The hash from the snapshot.</param>
    /// <param name="currentHash">The current event reducer hash.</param>
    [LoggerMessage(
        6,
        LogLevel.Information,
        "Reducer hash mismatch for aggregate {AggregateKey}: snapshot has {SnapshotHash}, current is {CurrentHash}")]
    public static partial void ReducerHashMismatch(
        this ILogger logger,
        string aggregateKey,
        string snapshotHash,
        string currentHash
    );

    /// <summary>
    ///     Logs when requesting background snapshot persistence.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="aggregateKey">The aggregate key.</param>
    /// <param name="version">The snapshot version.</param>
    [LoggerMessage(
        7,
        LogLevel.Debug,
        "Requesting background snapshot persistence for aggregate {AggregateKey} at version {Version}")]
    public static partial void RequestingSnapshotPersistence(
        this ILogger logger,
        string aggregateKey,
        long version
    );

    /// <summary>
    ///     Logs when a failed step is converted into a compensating boundary.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="failedStepIndex">The failed step index.</param>
    /// <param name="fromStepIndex">The compensation starting index.</param>
    /// <param name="aggregateKey">The aggregate key.</param>
    [LoggerMessage(
        20,
        LogLevel.Debug,
        "Reminder appending compensation from step {FromStepIndex} after failed step {FailedStepIndex} for aggregate {AggregateKey}")]
    public static partial void SagaReminderAppendingCompensation(
        this ILogger logger,
        int failedStepIndex,
        int fromStepIndex,
        string aggregateKey
    );

    /// <summary>
    ///     Logs when reminder replay fails.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="reminderName">The reminder name.</param>
    /// <param name="aggregateKey">The aggregate key.</param>
    /// <param name="exception">The exception.</param>
    [LoggerMessage(21, LogLevel.Error, "Reminder {ReminderName} failed for aggregate {AggregateKey}")]
    public static partial void SagaReminderFailed(
        this ILogger logger,
        string reminderName,
        string aggregateKey,
        Exception exception
    );

    /// <summary>
    ///     Logs when a reminder with an unknown name is ignored.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="reminderName">The reminder name.</param>
    /// <param name="aggregateKey">The aggregate key.</param>
    [LoggerMessage(11, LogLevel.Debug, "Ignoring unknown reminder {ReminderName} for aggregate {AggregateKey}")]
    public static partial void SagaReminderIgnoredUnknown(
        this ILogger logger,
        string reminderName,
        string aggregateKey
    );

    /// <summary>
    ///     Logs when the reminder callback finds no confirmed cursor.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="reminderName">The reminder name.</param>
    /// <param name="aggregateKey">The aggregate key.</param>
    [LoggerMessage(
        14,
        LogLevel.Debug,
        "Reminder {ReminderName} found no confirmed cursor for aggregate {AggregateKey}")]
    public static partial void SagaReminderNoConfirmedCursor(
        this ILogger logger,
        string reminderName,
        string aggregateKey
    );

    /// <summary>
    ///     Logs when the callback cannot load the snapshot at the confirmed position.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="aggregateKey">The aggregate key.</param>
    /// <param name="confirmedPosition">The confirmed position.</param>
    [LoggerMessage(
        15,
        LogLevel.Warning,
        "Reminder could not load a snapshot at confirmed position {ConfirmedPosition} for aggregate {AggregateKey}")]
    public static partial void SagaReminderNoSnapshot(
        this ILogger logger,
        string aggregateKey,
        long confirmedPosition
    );

    /// <summary>
    ///     Logs when the deterministic saga reminder is being registered or updated.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="reminderName">The reminder name.</param>
    /// <param name="aggregateKey">The aggregate key.</param>
    [LoggerMessage(13, LogLevel.Debug, "Registering or updating reminder {ReminderName} for aggregate {AggregateKey}")]
    public static partial void SagaReminderRegistering(
        this ILogger logger,
        string reminderName,
        string aggregateKey
    );

    /// <summary>
    ///     Logs when replay resumes from a safe lifecycle boundary.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="boundaryType">The boundary event type.</param>
    /// <param name="aggregateKey">The aggregate key.</param>
    /// <param name="eventPosition">The persisted event position.</param>
    [LoggerMessage(
        16,
        LogLevel.Debug,
        "Reminder replaying boundary {BoundaryType} at position {EventPosition} for aggregate {AggregateKey}")]
    public static partial void SagaReminderReplayingBoundary(
        this ILogger logger,
        string boundaryType,
        string aggregateKey,
        long eventPosition
    );

    /// <summary>
    ///     Logs when a reminder tick is skipped because saga work is already in progress.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="reminderName">The reminder name.</param>
    /// <param name="aggregateKey">The aggregate key.</param>
    [LoggerMessage(
        12,
        LogLevel.Debug,
        "Skipping reminder {ReminderName} for aggregate {AggregateKey} because saga work is already active")]
    public static partial void SagaReminderSkippedAlreadyActive(
        this ILogger logger,
        string reminderName,
        string aggregateKey
    );

    /// <summary>
    ///     Logs when a terminal saga tail event causes reminder unregistration.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="eventType">The terminal tail event type.</param>
    /// <param name="aggregateKey">The aggregate key.</param>
    /// <param name="reminderName">The reminder name.</param>
    [LoggerMessage(
        18,
        LogLevel.Information,
        "Reminder {ReminderName} found terminal tail event {EventType} for aggregate {AggregateKey}; unregistering")]
    public static partial void SagaReminderTerminalEvent(
        this ILogger logger,
        string eventType,
        string aggregateKey,
        string reminderName
    );

    /// <summary>
    ///     Logs when terminal saga state causes reminder unregistration.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="aggregateKey">The aggregate key.</param>
    /// <param name="phase">The terminal saga phase.</param>
    /// <param name="reminderName">The reminder name.</param>
    [LoggerMessage(
        17,
        LogLevel.Information,
        "Reminder {ReminderName} found terminal phase {Phase} for aggregate {AggregateKey}; unregistering")]
    public static partial void SagaReminderTerminalState(
        this ILogger logger,
        string aggregateKey,
        SagaPhase phase,
        string reminderName
    );

    /// <summary>
    ///     Logs when a business or otherwise unsafe tail event is encountered.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="eventType">The tail event type.</param>
    /// <param name="aggregateKey">The aggregate key.</param>
    [LoggerMessage(
        19,
        LogLevel.Warning,
        "Reminder found unsupported tail event {EventType} for aggregate {AggregateKey}; no replay will occur")]
    public static partial void SagaReminderUnsafeTail(
        this ILogger logger,
        string eventType,
        string aggregateKey
    );

    /// <summary>
    ///     Logs when a snapshot is successfully loaded.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="aggregateKey">The aggregate key.</param>
    /// <param name="version">The snapshot version.</param>
    [LoggerMessage(8, LogLevel.Debug, "Loaded snapshot for aggregate {AggregateKey} at version {Version}")]
    public static partial void SnapshotLoaded(
        this ILogger logger,
        string aggregateKey,
        long version
    );

    /// <summary>
    ///     Logs when state hydration completes.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="aggregateKey">The aggregate key.</param>
    /// <param name="finalPosition">The final position after hydration.</param>
    [LoggerMessage(9, LogLevel.Debug, "State hydrated for aggregate {AggregateKey} at position {FinalPosition}")]
    public static partial void StateHydrated(
        this ILogger logger,
        string aggregateKey,
        long finalPosition
    );
}