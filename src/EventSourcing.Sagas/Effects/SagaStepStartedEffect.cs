using System;
using System.Collections.Generic;
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
///     Event effect that executes a saga step when <see cref="SagaStepStartedEvent" /> is emitted.
/// </summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
internal sealed class SagaStepStartedEffect<TSaga> : EventEffectBase<SagaStepStartedEvent, TSaga>
    where TSaga : class
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SagaStepStartedEffect{TSaga}" /> class.
    /// </summary>
    /// <param name="stepRegistry">The saga step registry.</param>
    /// <param name="serviceProvider">The service provider for resolving step instances.</param>
    /// <param name="timeProvider">The time provider for timestamps.</param>
    /// <param name="logger">The logger.</param>
    public SagaStepStartedEffect(
        ISagaStepRegistry<TSaga> stepRegistry,
        IServiceProvider serviceProvider,
        TimeProvider timeProvider,
        ILogger<SagaStepStartedEffect<TSaga>> logger
    )
    {
        StepRegistry = stepRegistry;
        ServiceProvider = serviceProvider;
        TimeProvider = timeProvider;
        Logger = logger;
    }

    private ILogger<SagaStepStartedEffect<TSaga>> Logger { get; }

    private IServiceProvider ServiceProvider { get; }

    private ISagaStepRegistry<TSaga> StepRegistry { get; }

    private TimeProvider TimeProvider { get; }

    private static async Task<StepResult> ExecuteStepAsync(
        object stepInstance,
        ISagaContext context,
        TSaga state,
        CancellationToken cancellationToken
    )
    {
        // Use reflection to call ExecuteAsync on the step base class
        // This is needed because we don't know the concrete step type at compile time
        Type stepType = stepInstance.GetType();
        MethodInfo? executeMethod = stepType.GetMethod(
            nameof(SagaStepBase<TSaga>.ExecuteAsync),
            [typeof(ISagaContext), typeof(TSaga), typeof(CancellationToken)]);
        if (executeMethod is null)
        {
            return StepResult.Failed("INVALID_STEP", "Step does not have an ExecuteAsync method.");
        }

        object? taskResult = executeMethod.Invoke(stepInstance, [context, state, cancellationToken]);
        if (taskResult is Task<StepResult> typedTask)
        {
            return await typedTask;
        }

        return StepResult.Failed("INVALID_STEP_RESULT", "Step ExecuteAsync did not return Task<StepResult>.");
    }

    private static (Guid SagaId, string CorrelationId, DateTimeOffset StartedAt, string? StepHash) ExtractSagaIdentity(
        TSaga currentState
    )
    {
        if (currentState is ISagaState sagaState)
        {
            return (sagaState.SagaId, sagaState.CorrelationId ?? string.Empty,
                sagaState.StartedAt ?? DateTimeOffset.UtcNow, sagaState.StepHash);
        }

        // Fallback when state doesn't implement ISagaState
        return (Guid.Empty, string.Empty, DateTimeOffset.UtcNow, null);
    }

    private static bool IsCriticalException(
        Exception ex
    ) =>
        ex is OutOfMemoryException or StackOverflowException or ThreadAbortException;

    /// <inheritdoc />
    public override async IAsyncEnumerable<object> HandleAsync(
        SagaStepStartedEvent eventData,
        TSaga currentState,
        string brookKey,
        long eventPosition,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        _ = brookKey; // Saga effects don't use brook key directly
        _ = eventPosition; // Saga effects don't use event position directly

        // Extract saga identity from state if available
        (Guid sagaId, string correlationId, DateTimeOffset startedAt, string? stepHash) =
            ExtractSagaIdentity(currentState);

        // Idempotency guard: skip if step was already completed
        if (currentState is ISagaState sagaState &&
            (sagaState.LastCompletedStepIndex >= GetStepIndex(eventData.StepOrder)))
        {
            Logger.SagaStepCompleted(sagaId, eventData.StepName, eventData.StepOrder);
            yield break;
        }

        // Step hash validation: detect definition drift
        if (!string.IsNullOrEmpty(stepHash) && (stepHash != StepRegistry.StepHash))
        {
            Logger.SagaStepFailed(
                sagaId,
                eventData.StepName,
                eventData.StepOrder,
                "STEP_HASH_MISMATCH",
                $"Saga step hash '{stepHash}' does not match registry hash '{StepRegistry.StepHash}'.");
            yield return new SagaStepFailedEvent(
                eventData.StepName,
                eventData.StepOrder,
                "STEP_HASH_MISMATCH",
                $"Saga step definitions have changed since saga started. Expected '{stepHash}', got '{StepRegistry.StepHash}'.",
                TimeProvider.GetUtcNow());
            yield break;
        }

        ISagaStepInfo? stepInfo = FindStep(eventData.StepOrder);
        if (stepInfo is null)
        {
            Logger.StepNotFound(eventData.StepName, eventData.StepOrder);
            yield return new SagaStepFailedEvent(
                eventData.StepName,
                eventData.StepOrder,
                "STEP_NOT_FOUND",
                $"Step '{eventData.StepName}' with order {eventData.StepOrder} not found in registry.",
                TimeProvider.GetUtcNow());
            yield break;
        }

        // Execute step and collect results (no yield inside try/catch)
        List<object> eventsToYield = await ExecuteStepAndCollectEventsAsync(
            stepInfo,
            currentState,
            sagaId,
            correlationId,
            startedAt,
            cancellationToken);

        // Yield all collected events outside try/catch
        foreach (object evt in eventsToYield)
        {
            yield return evt;
        }
    }

    private async Task<List<object>> ExecuteStepAndCollectEventsAsync(
        ISagaStepInfo stepInfo,
        TSaga currentState,
        Guid sagaId,
        string correlationId,
        DateTimeOffset startedAt,
        CancellationToken cancellationToken
    )
    {
        List<object> eventsToYield = [];

        // Resolve the step instance from DI
        object stepInstance = ServiceProvider.GetRequiredService(stepInfo.StepType);

        // Create saga context with proper identity from saga state
        SagaContext context = new()
        {
            SagaId = sagaId,
            CorrelationId = correlationId,
            SagaName = typeof(TSaga).Name,
            Attempt = 1,
            StartedAt = startedAt,
        };
        try
        {
            // Execute the step
            StepResult result = await ExecuteStepAsync(stepInstance, context, currentState, cancellationToken);
            if (result.Success)
            {
                // Collect business events from the step
                eventsToYield.AddRange(result.Events);

                // Add step completed event
                eventsToYield.Add(new SagaStepCompletedEvent(stepInfo.Name, stepInfo.Order, TimeProvider.GetUtcNow()));
            }
            else
            {
                eventsToYield.Add(
                    new SagaStepFailedEvent(
                        stepInfo.Name,
                        stepInfo.Order,
                        result.ErrorCode ?? "STEP_FAILED",
                        result.ErrorMessage,
                        TimeProvider.GetUtcNow()));
            }
        }
        catch (Exception ex) when (!IsCriticalException(ex))
        {
            Logger.StepExecutionFailed(stepInfo.Name, stepInfo.Order, ex);
            eventsToYield.Add(
                new SagaStepFailedEvent(
                    stepInfo.Name,
                    stepInfo.Order,
                    "STEP_EXCEPTION",
                    ex.Message,
                    TimeProvider.GetUtcNow()));
        }

        return eventsToYield;
    }

    private ISagaStepInfo? FindStep(
        int stepOrder
    )
    {
        foreach (ISagaStepInfo step in StepRegistry.Steps)
        {
            if (step.Order == stepOrder)
            {
                return step;
            }
        }

        return null;
    }

    private int GetStepIndex(
        int stepOrder
    )
    {
        IReadOnlyList<ISagaStepInfo> steps = StepRegistry.Steps;
        for (int i = 0; i < steps.Count; i++)
        {
            if (steps[i].Order == stepOrder)
            {
                return i;
            }
        }

        return -1;
    }
}