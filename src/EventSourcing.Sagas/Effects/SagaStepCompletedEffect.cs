using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions.Events;


namespace Mississippi.EventSourcing.Sagas.Effects;

/// <summary>
///     Event effect that handles step completion and progresses to the next step.
/// </summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
internal sealed class SagaStepCompletedEffect<TSaga> : EventEffectBase<SagaStepCompletedEvent, TSaga>
    where TSaga : class
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SagaStepCompletedEffect{TSaga}" /> class.
    /// </summary>
    /// <param name="stepRegistry">The saga step registry.</param>
    /// <param name="timeProvider">The time provider for timestamps.</param>
    /// <param name="logger">The logger.</param>
    public SagaStepCompletedEffect(
        ISagaStepRegistry<TSaga> stepRegistry,
        TimeProvider timeProvider,
        ILogger<SagaStepCompletedEffect<TSaga>> logger
    )
    {
        StepRegistry = stepRegistry;
        TimeProvider = timeProvider;
        Logger = logger;
    }

    private ILogger<SagaStepCompletedEffect<TSaga>> Logger { get; }

    private ISagaStepRegistry<TSaga> StepRegistry { get; }

    private TimeProvider TimeProvider { get; }

    /// <inheritdoc />
    public override async IAsyncEnumerable<object> HandleAsync(
        SagaStepCompletedEvent eventData,
        TSaga currentState,
        string brookKey,
        long eventPosition,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        _ = brookKey; // Saga effects don't use brook key directly
        _ = eventPosition; // Saga effects don't use event position directly
        await Task.CompletedTask; // Async enumerable requires async method

        // Extract saga identity from state if available
        Guid sagaId = currentState is ISagaState sagaState ? sagaState.SagaId : Guid.Empty;

        // Find the next step
        ISagaStepInfo? nextStep = FindNextStep(eventData.StepOrder);
        if (nextStep is null)
        {
            // No more steps - saga is complete
            Logger.SagaCompleted(sagaId, typeof(TSaga).Name);
            yield return new SagaCompletedEvent(TimeProvider.GetUtcNow());
            yield break;
        }

        // Start the next step
        Logger.SagaStepStarting(sagaId, nextStep.Name, nextStep.Order);
        yield return new SagaStepStartedEvent(nextStep.Name, nextStep.Order, TimeProvider.GetUtcNow());
    }

    private ISagaStepInfo? FindNextStep(
        int currentStepOrder
    )
    {
        IReadOnlyList<ISagaStepInfo> steps = StepRegistry.Steps;

        // Find the first step with order greater than the current step
        foreach (ISagaStepInfo step in steps)
        {
            if (step.Order > currentStepOrder)
            {
                return step;
            }
        }

        return null;
    }
}