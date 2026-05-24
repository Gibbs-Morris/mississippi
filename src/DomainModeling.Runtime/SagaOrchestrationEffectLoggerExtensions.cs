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
    /// <param name="stepName">The step name.</param>
    /// <param name="stepIndex">The step index.</param>
    /// <param name="exception">The exception thrown by the compensation step.</param>
    [LoggerMessage(4, LogLevel.Error, "Saga step {StepName} ({StepIndex}) for {SagaType} threw during compensation")]
    public static partial void SagaStepCompensationException(
        this ILogger logger,
        string sagaType,
        string stepName,
        int stepIndex,
        Exception exception
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
    /// <param name="stepName">The step name.</param>
    /// <param name="stepIndex">The step index.</param>
    /// <param name="exception">The exception thrown by the saga step.</param>
    [LoggerMessage(3, LogLevel.Error, "Saga step {StepName} ({StepIndex}) for {SagaType} threw during execution")]
    public static partial void SagaStepExecutionException(
        this ILogger logger,
        string sagaType,
        string stepName,
        int stepIndex,
        Exception exception
    );
}