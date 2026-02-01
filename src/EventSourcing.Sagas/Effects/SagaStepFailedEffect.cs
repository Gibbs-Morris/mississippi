using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions.Events;


namespace Mississippi.EventSourcing.Sagas.Effects;

/// <summary>
///     Event effect that handles step failure and triggers compensation.
/// </summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
internal sealed class SagaStepFailedEffect<TSaga> : EventEffectBase<SagaStepFailedEvent, TSaga>
    where TSaga : class
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SagaStepFailedEffect{TSaga}" /> class.
    /// </summary>
    /// <param name="stepRegistry">The saga step registry.</param>
    /// <param name="serviceProvider">The service provider for resolving compensation instances.</param>
    /// <param name="timeProvider">The time provider for timestamps.</param>
    /// <param name="logger">The logger.</param>
    public SagaStepFailedEffect(
        ISagaStepRegistry<TSaga> stepRegistry,
        IServiceProvider serviceProvider,
        TimeProvider timeProvider,
        ILogger<SagaStepFailedEffect<TSaga>> logger
    )
    {
        StepRegistry = stepRegistry;
        ServiceProvider = serviceProvider;
        TimeProvider = timeProvider;
        Logger = logger;
    }

    private ILogger<SagaStepFailedEffect<TSaga>> Logger { get; }

    private IServiceProvider ServiceProvider { get; }

    private ISagaStepRegistry<TSaga> StepRegistry { get; }

    private TimeProvider TimeProvider { get; }

    private static CompensationStrategy GetCompensationStrategy()
    {
        SagaOptionsAttribute? options = typeof(TSaga).GetCustomAttribute<SagaOptionsAttribute>();
        return options?.CompensationStrategy ?? CompensationStrategy.Immediate;
    }

    private static int GetMaxRetries()
    {
        SagaOptionsAttribute? options = typeof(TSaga).GetCustomAttribute<SagaOptionsAttribute>();
        return options?.MaxRetries ?? 3;
    }

    private static bool IsCriticalException(
        Exception ex
    ) =>
        ex is OutOfMemoryException or StackOverflowException or ThreadAbortException;

    /// <inheritdoc />
    public override async IAsyncEnumerable<object> HandleAsync(
        SagaStepFailedEvent eventData,
        TSaga currentState,
        string brookKey,
        long eventPosition,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        _ = brookKey; // Saga effects don't use brook key directly
        _ = eventPosition; // Saga effects don't use event position directly

        // Extract saga identity from state if available
        Guid sagaId = currentState is ISagaState sagaState ? sagaState.SagaId : Guid.Empty;

        // Get compensation strategy from saga options attribute
        CompensationStrategy strategy = GetCompensationStrategy();
        switch (strategy)
        {
            case CompensationStrategy.Immediate:
                // Start compensation for completed steps
                yield return new SagaCompensatingEvent(eventData.StepName, TimeProvider.GetUtcNow());

                // Run compensations in reverse order for steps completed before the failed one
                await foreach (object evt in RunCompensationsAsync(
                                   eventData.StepOrder,
                                   currentState,
                                   sagaId,
                                   cancellationToken))
                {
                    yield return evt;
                }

                yield return new SagaFailedEvent(
                    $"Step '{eventData.StepName}' failed: {eventData.ErrorCode} - {eventData.ErrorMessage}",
                    TimeProvider.GetUtcNow());
                break;
            case CompensationStrategy.Manual:
                // Just fail the saga without compensation
                yield return new SagaFailedEvent(
                    $"Step '{eventData.StepName}' failed: {eventData.ErrorCode} - {eventData.ErrorMessage}",
                    TimeProvider.GetUtcNow());
                break;
            case CompensationStrategy.RetryThenCompensate:
                int maxRetries = GetMaxRetries();
                int currentAttempt = currentState is ISagaState retryState ? retryState.CurrentStepAttempt : 1;
                if (currentAttempt < maxRetries)
                {
                    // Retry the step - emit SagaStepRetryEvent (increments attempt) then re-trigger step execution
                    int nextAttempt = currentAttempt + 1;
                    Logger.SagaStepRetrying(
                        sagaId,
                        eventData.StepName,
                        nextAttempt,
                        maxRetries,
                        eventData.ErrorCode ?? "UNKNOWN");
                    yield return new SagaStepRetryEvent(
                        eventData.StepName,
                        eventData.StepOrder,
                        nextAttempt,
                        maxRetries,
                        eventData.ErrorCode ?? "UNKNOWN",
                        eventData.ErrorMessage,
                        TimeProvider.GetUtcNow());

                    // Re-emit SagaStepStartedEvent to trigger the step again
                    yield return new SagaStepStartedEvent(
                        eventData.StepName,
                        eventData.StepOrder,
                        TimeProvider.GetUtcNow());
                }
                else
                {
                    // Max retries exceeded - compensate
                    Logger.SagaMaxRetriesExceeded(sagaId, eventData.StepName, maxRetries);
                    yield return new SagaCompensatingEvent(eventData.StepName, TimeProvider.GetUtcNow());
                    await foreach (object evt in RunCompensationsAsync(
                                       eventData.StepOrder,
                                       currentState,
                                       sagaId,
                                       cancellationToken))
                    {
                        yield return evt;
                    }

                    yield return new SagaFailedEvent(
                        $"Step '{eventData.StepName}' failed after {maxRetries} attempts: {eventData.ErrorCode} - {eventData.ErrorMessage}",
                        TimeProvider.GetUtcNow());
                }

                break;
        }
    }

    private async Task<CompensationResult> ExecuteCompensationAsync(
        ISagaStepInfo step,
        TSaga currentState,
        Guid sagaId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            object compensationInstance = ServiceProvider.GetRequiredService(step.CompensationType!);

            // Extract correlation ID and start time from saga state if available
            string correlationId = currentState is ISagaState sagaState
                ? sagaState.CorrelationId ?? string.Empty
                : string.Empty;
            DateTimeOffset startedAt = currentState is ISagaState ss
                ? ss.StartedAt ?? TimeProvider.GetUtcNow()
                : TimeProvider.GetUtcNow();

            // Create saga context for compensation with proper identity
            SagaContext context = new()
            {
                SagaId = sagaId,
                CorrelationId = correlationId,
                SagaName = typeof(TSaga).Name,
                Attempt = 1,
                StartedAt = startedAt,
            };

            // Use reflection to call CompensateAsync
            MethodInfo? compensateMethod = step.CompensationType!.GetMethod(
                nameof(SagaCompensationBase<TSaga>.CompensateAsync),
                [typeof(ISagaContext), typeof(TSaga), typeof(CancellationToken)]);
            if (compensateMethod is null)
            {
                return CompensationResult.Failed(
                    "INVALID_COMPENSATION",
                    "Compensation does not have a CompensateAsync method.");
            }

            object? taskResult = compensateMethod.Invoke(
                compensationInstance,
                [context, currentState, cancellationToken]);
            if (taskResult is Task<CompensationResult> typedTask)
            {
                return await typedTask;
            }

            return CompensationResult.Failed(
                "INVALID_COMPENSATION_RESULT",
                "Compensation did not return Task<CompensationResult>.");
        }
        catch (Exception ex) when (!IsCriticalException(ex))
        {
            Logger.SagaCompensationException(Guid.Empty, step.Name, ex);
            return CompensationResult.Failed("COMPENSATION_EXCEPTION", ex.Message);
        }
    }

    private async IAsyncEnumerable<object> RunCompensationsAsync(
        int failedStepOrder,
        TSaga currentState,
        Guid sagaId,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        IReadOnlyList<ISagaStepInfo> steps = StepRegistry.Steps;

        // Find steps that completed before the failed step (in reverse order for compensation)
        List<ISagaStepInfo> stepsToCompensate = steps.Where(step => step.Order < failedStepOrder).ToList();

        // Reverse to compensate in reverse order
        stepsToCompensate.Reverse();
        foreach (ISagaStepInfo step in stepsToCompensate)
        {
            if (step.CompensationType is null)
            {
                // No compensation defined for this step
                continue;
            }

            CompensationResult result = await ExecuteCompensationAsync(step, currentState, sagaId, cancellationToken);
            if (result.Success)
            {
                yield return new SagaStepCompensatedEvent(step.Name, step.Order, TimeProvider.GetUtcNow());
            }
            else
            {
                Logger.SagaCompensationException(sagaId, step.Name, new InvalidOperationException(result.ErrorMessage));

                // Emit explicit compensation failure event for observability
                yield return new SagaStepCompensationFailedEvent(
                    step.Name,
                    step.Order,
                    result.ErrorCode ?? "COMPENSATION_FAILED",
                    result.ErrorMessage,
                    TimeProvider.GetUtcNow());

                // Continue compensating other steps even if one fails
            }
        }
    }
}