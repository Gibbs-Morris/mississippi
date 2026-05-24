using System;

using Microsoft.Extensions.Logging;


namespace Mississippi.DomainModeling.Runtime;

/// <summary>
///     High-performance logging helpers for <see cref="SagaOrchestrationEffect{TSaga}" />.
/// </summary>
internal static partial class SagaOrchestrationEffectLoggerExtensions
{
    /// <summary>
    ///     Logs that a saga step compensation is executing.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="sagaType">The saga type name.</param>
    /// <param name="stepName">The step name.</param>
    /// <param name="stepIndex">The step index.</param>
    [LoggerMessage(2, LogLevel.Debug, "Compensating saga step {StepName} ({StepIndex}) for {SagaType}")]
    public static partial void SagaStepCompensating(
        this ILogger logger,
        string sagaType,
        string stepName,
        int stepIndex
    );

    /// <summary>
    ///     Logs that a saga step threw during compensation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="sagaType">The saga type name.</param>
    /// <param name="sagaId">The saga identifier.</param>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="brookKey">The aggregate brook key.</param>
    /// <param name="eventPosition">The lifecycle event position.</param>
    /// <param name="stepName">The step name.</param>
    /// <param name="stepIndex">The step index.</param>
    /// <param name="errorCode">The compensation exception error code.</param>
    /// <param name="exception">The exception thrown by the compensation step.</param>
    [LoggerMessage(
        4,
        LogLevel.Error,
        "Saga step {StepName} ({StepIndex}) for {SagaType} threw during compensation with {ErrorCode}; saga {SagaId} correlation {CorrelationId} aggregate {BrookKey} at event position {EventPosition}")]
    public static partial void SagaStepCompensationException(
        this ILogger logger,
        string sagaType,
        Guid sagaId,
        string? correlationId,
        string brookKey,
        long eventPosition,
        string stepName,
        int stepIndex,
        string errorCode,
        Exception exception
    );

    /// <summary>
    ///     Logs that saga compensation failed and the saga will be marked failed.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="sagaType">The saga type name.</param>
    /// <param name="sagaId">The saga identifier.</param>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="brookKey">The aggregate brook key.</param>
    /// <param name="eventPosition">The lifecycle event position.</param>
    /// <param name="stepName">The step name.</param>
    /// <param name="stepIndex">The step index.</param>
    /// <param name="errorCode">The compensation failure error code.</param>
    /// <param name="errorMessage">The compensation failure error message.</param>
    [LoggerMessage(
        6,
        LogLevel.Error,
        "Saga compensation step {StepName} ({StepIndex}) for {SagaType} failed with {ErrorCode}; saga {SagaId} correlation {CorrelationId} aggregate {BrookKey} at event position {EventPosition} will fail: {ErrorMessage}")]
    public static partial void SagaStepCompensationFailed(
        this ILogger logger,
        string sagaType,
        Guid sagaId,
        string? correlationId,
        string brookKey,
        long eventPosition,
        string stepName,
        int stepIndex,
        string errorCode,
        string? errorMessage
    );

    /// <summary>
    ///     Logs that saga compensation cannot continue because step metadata is missing.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="sagaType">The saga type name.</param>
    /// <param name="sagaId">The saga identifier.</param>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="brookKey">The aggregate brook key.</param>
    /// <param name="eventPosition">The lifecycle event position.</param>
    /// <param name="stepIndex">The step index.</param>
    /// <param name="errorCode">The saga failure error code.</param>
    /// <param name="errorMessage">The saga failure error message.</param>
    [LoggerMessage(
        8,
        LogLevel.Error,
        "Saga compensation for {SagaType} cannot find step metadata for step {StepIndex}; saga {SagaId} correlation {CorrelationId} aggregate {BrookKey} at event position {EventPosition} will fail: {ErrorCode} - {ErrorMessage}")]
    public static partial void SagaStepCompensationMetadataMissing(
        this ILogger logger,
        string sagaType,
        Guid sagaId,
        string? correlationId,
        string brookKey,
        long eventPosition,
        int stepIndex,
        string errorCode,
        string errorMessage
    );

    /// <summary>
    ///     Logs that a saga step is executing.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="sagaType">The saga type name.</param>
    /// <param name="stepName">The step name.</param>
    /// <param name="stepIndex">The step index.</param>
    [LoggerMessage(1, LogLevel.Debug, "Executing saga step {StepName} ({StepIndex}) for {SagaType}")]
    public static partial void SagaStepExecuting(
        this ILogger logger,
        string sagaType,
        string stepName,
        int stepIndex
    );

    /// <summary>
    ///     Logs that a saga step threw during execution.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="sagaType">The saga type name.</param>
    /// <param name="sagaId">The saga identifier.</param>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="brookKey">The aggregate brook key.</param>
    /// <param name="eventPosition">The lifecycle event position.</param>
    /// <param name="stepName">The step name.</param>
    /// <param name="stepIndex">The step index.</param>
    /// <param name="errorCode">The step exception error code.</param>
    /// <param name="exception">The exception thrown by the saga step.</param>
    [LoggerMessage(
        3,
        LogLevel.Error,
        "Saga step {StepName} ({StepIndex}) for {SagaType} threw during execution with {ErrorCode}; saga {SagaId} correlation {CorrelationId} aggregate {BrookKey} at event position {EventPosition}")]
    public static partial void SagaStepExecutionException(
        this ILogger logger,
        string sagaType,
        Guid sagaId,
        string? correlationId,
        string brookKey,
        long eventPosition,
        string stepName,
        int stepIndex,
        string errorCode,
        Exception exception
    );

    /// <summary>
    ///     Logs that saga step execution failed and compensation will begin.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="sagaType">The saga type name.</param>
    /// <param name="sagaId">The saga identifier.</param>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="brookKey">The aggregate brook key.</param>
    /// <param name="eventPosition">The lifecycle event position.</param>
    /// <param name="stepName">The step name.</param>
    /// <param name="stepIndex">The step index.</param>
    /// <param name="errorCode">The step failure error code.</param>
    /// <param name="errorMessage">The step failure error message.</param>
    [LoggerMessage(
        5,
        LogLevel.Error,
        "Saga step {StepName} ({StepIndex}) for {SagaType} failed with {ErrorCode}; saga {SagaId} correlation {CorrelationId} aggregate {BrookKey} at event position {EventPosition} will compensate: {ErrorMessage}")]
    public static partial void SagaStepExecutionFailed(
        this ILogger logger,
        string sagaType,
        Guid sagaId,
        string? correlationId,
        string brookKey,
        long eventPosition,
        string stepName,
        int stepIndex,
        string errorCode,
        string? errorMessage
    );

    /// <summary>
    ///     Logs that saga execution cannot start because step metadata is missing.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="sagaType">The saga type name.</param>
    /// <param name="sagaId">The saga identifier.</param>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="brookKey">The aggregate brook key.</param>
    /// <param name="eventPosition">The lifecycle event position.</param>
    /// <param name="stepIndex">The step index.</param>
    /// <param name="errorCode">The saga failure error code.</param>
    /// <param name="errorMessage">The saga failure error message.</param>
    [LoggerMessage(
        7,
        LogLevel.Error,
        "Saga step metadata missing for {SagaType} step {StepIndex}; saga {SagaId} correlation {CorrelationId} aggregate {BrookKey} at event position {EventPosition} will fail: {ErrorCode} - {ErrorMessage}")]
    public static partial void SagaStepMetadataMissing(
        this ILogger logger,
        string sagaType,
        Guid sagaId,
        string? correlationId,
        string brookKey,
        long eventPosition,
        int stepIndex,
        string errorCode,
        string errorMessage
    );
}