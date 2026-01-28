using System;
using System.Collections.Generic;

using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions.Commands;
using Mississippi.EventSourcing.Sagas.Abstractions.Events;


namespace Mississippi.EventSourcing.Sagas;

/// <summary>
///     Command handler for starting a saga.
/// </summary>
/// <typeparam name="TInput">The saga input type.</typeparam>
/// <typeparam name="TSaga">The saga state type.</typeparam>
internal sealed class StartSagaCommandHandler<TInput, TSaga> : CommandHandlerBase<StartSagaCommand<TInput>, TSaga>
    where TInput : class
    where TSaga : class
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="StartSagaCommandHandler{TInput, TSaga}" /> class.
    /// </summary>
    /// <param name="stepRegistry">The saga step registry.</param>
    /// <param name="timeProvider">The time provider for timestamps.</param>
    public StartSagaCommandHandler(
        ISagaStepRegistry<TSaga> stepRegistry,
        TimeProvider timeProvider
    )
    {
        StepRegistry = stepRegistry;
        TimeProvider = timeProvider;
    }

    private ISagaStepRegistry<TSaga> StepRegistry { get; }

    private TimeProvider TimeProvider { get; }

    /// <inheritdoc />
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        StartSagaCommand<TInput> command,
        TSaga? state
    )
    {
        // Validate saga hasn't already started
        if (state is ISagaState sagaState && (sagaState.Phase != SagaPhase.NotStarted))
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                "SAGA_ALREADY_STARTED",
                "Cannot start a saga that has already been started.");
        }

        IReadOnlyList<ISagaStepInfo> steps = StepRegistry.Steps;
        if (steps.Count == 0)
        {
            return OperationResult.Fail<IReadOnlyList<object>>("NO_SAGA_STEPS", "Saga has no steps defined.");
        }

        DateTimeOffset now = TimeProvider.GetUtcNow();
        ISagaStepInfo firstStep = steps[0];

        // Emit saga started event and first step started event
        List<object> events =
        [
            new SagaStartedEvent(
                Guid.NewGuid().ToString(),
                typeof(TSaga).Name,
                StepRegistry.StepHash,
                command.CorrelationId,
                now),
            new SagaStepStartedEvent(firstStep.Name, firstStep.Order, now),
        ];
        return OperationResult.Ok<IReadOnlyList<object>>(events);
    }
}