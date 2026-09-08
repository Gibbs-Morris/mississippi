using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

using Microsoft.Extensions.Logging;

using Mississippi.DomainModeling.Abstractions;
using Mississippi.DomainModeling.Runtime.Sagas;


namespace Mississippi.DomainModeling.Runtime;

/// <summary>
///     Handles start commands for saga orchestration by emitting the saga-started event.
/// </summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
/// <typeparam name="TInput">The saga input type.</typeparam>
public sealed class StartSagaCommandHandler<TSaga, TInput> : CommandHandlerBase<StartSagaCommand<TInput>, TSaga>
    where TSaga : class, ISagaState
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="StartSagaCommandHandler{TSaga,TInput}" /> class.
    /// </summary>
    /// <param name="stepInfoProvider">The saga step metadata provider.</param>
    /// <param name="timeProvider">The time provider.</param>
    /// <param name="logger">The logger.</param>
    public StartSagaCommandHandler(
        ISagaStepInfoProvider<TSaga> stepInfoProvider,
        TimeProvider timeProvider,
        ILogger<StartSagaCommandHandler<TSaga, TInput>>? logger = null
    )
    {
        ArgumentNullException.ThrowIfNull(stepInfoProvider);
        ArgumentNullException.ThrowIfNull(timeProvider);
        StepInfoProvider = stepInfoProvider;
        TimeProvider = timeProvider;
        Logger = logger;
    }

    private ILogger<StartSagaCommandHandler<TSaga, TInput>>? Logger { get; }

    private ISagaStepInfoProvider<TSaga> StepInfoProvider { get; }

    private TimeProvider TimeProvider { get; }

    /// <inheritdoc />
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        StartSagaCommand<TInput> command,
        TSaga? state
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        if (state is not null && (state.Phase != SagaPhase.NotStarted))
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidState,
                $"Saga '{typeof(TSaga).Name}' has already started.");
        }

        string stepHash;
        try
        {
            ImmutableArray<SagaStepInfo> steps = StepInfoProvider.Steps.ToImmutableArray();
            if (steps.IsEmpty)
            {
                return OperationResult.Fail<IReadOnlyList<object>>(
                    AggregateErrorCodes.InvalidState,
                    $"Saga '{typeof(TSaga).Name}' has no registered steps.");
            }

            stepHash = SagaStepHash.Compute(steps);
        }
        catch (Exception exception) when (exception is not (OutOfMemoryException or StackOverflowException
                                              or ThreadInterruptedException or OperationCanceledException))
        {
            Logger?.SagaStartMetadataInvalid(typeof(TSaga).Name, command.SagaId, exception);
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidState,
                "The registered saga workflow metadata is invalid.");
        }

        SagaStartedEvent started = new()
        {
            SagaId = command.SagaId,
            StepHash = stepHash,
            StartedAt = TimeProvider.GetUtcNow(),
            CorrelationId = command.CorrelationId,
        };
        SagaInputProvided<TInput> inputProvided = new()
        {
            SagaId = command.SagaId,
            Input = command.Input,
        };
        return OperationResult.Ok<IReadOnlyList<object>>([started, inputProvided]);
    }
}