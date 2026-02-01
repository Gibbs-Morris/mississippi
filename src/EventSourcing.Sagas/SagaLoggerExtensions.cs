using System;

using Microsoft.Extensions.Logging;


namespace Mississippi.EventSourcing.Sagas;

/// <summary>
///     High-performance logging extension methods for saga operations.
/// </summary>
internal static partial class SagaLoggerExtensions
{
    [LoggerMessage(
        EventId = 1017,
        Level = LogLevel.Debug,
        Message = "Saga {SagaId} applying post-step delay of {DelayMilliseconds}ms after step {StepName}")]
    public static partial void ApplyingPostStepDelay(
        this ILogger logger,
        Guid sagaId,
        string stepName,
        int delayMilliseconds
    );

    [LoggerMessage(
        EventId = 1018,
        Level = LogLevel.Debug,
        Message = "Saga {SagaId} post-step delay completed for step {StepName}")]
    public static partial void PostStepDelayCompleted(
        this ILogger logger,
        Guid sagaId,
        string stepName
    );

    [LoggerMessage(
        EventId = 1005,
        Level = LogLevel.Information,
        Message = "Saga {SagaId} ({SagaType}) cancelling: {Reason}")]
    public static partial void SagaCancelling(
        this ILogger logger,
        Guid sagaId,
        string sagaType,
        string reason
    );

    [LoggerMessage(
        EventId = 1010,
        Level = LogLevel.Information,
        Message = "Saga {SagaId} ({SagaType}) compensating from step index {FromStepIndex}")]
    public static partial void SagaCompensating(
        this ILogger logger,
        Guid sagaId,
        string sagaType,
        int fromStepIndex
    );

    [LoggerMessage(
        EventId = 1012,
        Level = LogLevel.Error,
        Message = "Saga {SagaId} compensation for step {StepName} threw exception")]
    public static partial void SagaCompensationException(
        this ILogger logger,
        Guid sagaId,
        string stepName,
        Exception exception
    );

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Information,
        Message = "Saga {SagaId} ({SagaType}) completed successfully")]
    public static partial void SagaCompleted(
        this ILogger logger,
        Guid sagaId,
        string sagaType
    );

    [LoggerMessage(EventId = 1003, Level = LogLevel.Warning, Message = "Saga {SagaId} ({SagaType}) failed: {Reason}")]
    public static partial void SagaFailed(
        this ILogger logger,
        Guid sagaId,
        string sagaType,
        string reason
    );

    [LoggerMessage(
        EventId = 1016,
        Level = LogLevel.Warning,
        Message = "Saga {SagaId} step {StepName} exhausted all {MaxRetries} retry attempts, compensating")]
    public static partial void SagaMaxRetriesExceeded(
        this ILogger logger,
        Guid sagaId,
        string stepName,
        int maxRetries
    );

    [LoggerMessage(EventId = 1004, Level = LogLevel.Information, Message = "Saga {SagaId} ({SagaType}) resuming")]
    public static partial void SagaResuming(
        this ILogger logger,
        Guid sagaId,
        string sagaType
    );

    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Saga {SagaId} ({SagaType}) starting")]
    public static partial void SagaStarting(
        this ILogger logger,
        Guid sagaId,
        string sagaType
    );

    [LoggerMessage(
        EventId = 1011,
        Level = LogLevel.Debug,
        Message = "Saga {SagaId} step {StepName} (order {StepOrder}) compensated")]
    public static partial void SagaStepCompensated(
        this ILogger logger,
        Guid sagaId,
        string stepName,
        int stepOrder
    );

    [LoggerMessage(
        EventId = 1007,
        Level = LogLevel.Debug,
        Message = "Saga {SagaId} step {StepName} (order {StepOrder}) completed")]
    public static partial void SagaStepCompleted(
        this ILogger logger,
        Guid sagaId,
        string stepName,
        int stepOrder
    );

    [LoggerMessage(
        EventId = 1009,
        Level = LogLevel.Error,
        Message = "Saga {SagaId} step {StepName} (order {StepOrder}) threw exception")]
    public static partial void SagaStepException(
        this ILogger logger,
        Guid sagaId,
        string stepName,
        int stepOrder,
        Exception exception
    );

    [LoggerMessage(
        EventId = 1008,
        Level = LogLevel.Warning,
        Message = "Saga {SagaId} step {StepName} (order {StepOrder}) failed: {ErrorCode} - {ErrorMessage}")]
    public static partial void SagaStepFailed(
        this ILogger logger,
        Guid sagaId,
        string stepName,
        int stepOrder,
        string? errorCode,
        string? errorMessage
    );

    [LoggerMessage(
        EventId = 1015,
        Level = LogLevel.Information,
        Message =
            "Saga {SagaId} step {StepName} retrying (attempt {AttemptNumber}/{MaxAttempts}) after error: {ErrorCode}")]
    public static partial void SagaStepRetrying(
        this ILogger logger,
        Guid sagaId,
        string stepName,
        int attemptNumber,
        int maxAttempts,
        string errorCode
    );

    [LoggerMessage(
        EventId = 1006,
        Level = LogLevel.Debug,
        Message = "Saga {SagaId} step {StepName} (order {StepOrder}) starting")]
    public static partial void SagaStepStarting(
        this ILogger logger,
        Guid sagaId,
        string stepName,
        int stepOrder
    );

    [LoggerMessage(
        EventId = 1014,
        Level = LogLevel.Error,
        Message = "Step {StepName} (order {StepOrder}) execution failed")]
    public static partial void StepExecutionFailed(
        this ILogger logger,
        string stepName,
        int stepOrder,
        Exception exception
    );

    [LoggerMessage(
        EventId = 1013,
        Level = LogLevel.Warning,
        Message = "Step {StepName} (order {StepOrder}) not found in saga step registry")]
    public static partial void StepNotFound(
        this ILogger logger,
        string stepName,
        int stepOrder
    );
}