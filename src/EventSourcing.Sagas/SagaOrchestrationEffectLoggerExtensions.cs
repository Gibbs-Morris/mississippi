using Microsoft.Extensions.Logging;

namespace Mississippi.EventSourcing.Sagas;

/// <summary>
///     High-performance logging helpers for <see cref="SagaOrchestrationEffect{TSaga}" />.
/// </summary>
internal static partial class SagaOrchestrationEffectLoggerExtensions
{
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
}
