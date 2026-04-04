using System;
using System.Collections.Generic;

using Mississippi.DomainModeling.Abstractions;


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
    /// <param name="sagaAccessContextProvider">Provider for the persisted saga access-context fingerprint.</param>
    /// <param name="stepInfoProvider">The saga step metadata provider.</param>
    /// <param name="recoveryInfoProvider">The saga recovery metadata provider.</param>
    /// <param name="timeProvider">The time provider.</param>
    public StartSagaCommandHandler(
        ISagaAccessContextProvider sagaAccessContextProvider,
        ISagaStepInfoProvider<TSaga> stepInfoProvider,
        ISagaRecoveryInfoProvider<TSaga> recoveryInfoProvider,
        TimeProvider timeProvider
    )
    {
        ArgumentNullException.ThrowIfNull(sagaAccessContextProvider);
        ArgumentNullException.ThrowIfNull(stepInfoProvider);
        ArgumentNullException.ThrowIfNull(recoveryInfoProvider);
        ArgumentNullException.ThrowIfNull(timeProvider);
        SagaAccessContextProvider = sagaAccessContextProvider;
        StepInfoProvider = stepInfoProvider;
        RecoveryInfoProvider = recoveryInfoProvider;
        TimeProvider = timeProvider;
    }

    private ISagaRecoveryInfoProvider<TSaga> RecoveryInfoProvider { get; }

    private ISagaAccessContextProvider SagaAccessContextProvider { get; }

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

        if (StepInfoProvider.Steps.Count == 0)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidState,
                $"Saga '{typeof(TSaga).Name}' has no registered steps.");
        }

        SagaStartedEvent started = new()
        {
            AccessContextFingerprint = SagaAccessContextProvider.GetFingerprint(),
            SagaId = command.SagaId,
            RecoveryMode = RecoveryInfoProvider.Recovery.Mode,
            RecoveryProfile = RecoveryInfoProvider.Recovery.Profile,
            StepHash = SagaStepHash.Compute(RecoveryInfoProvider.Recovery, StepInfoProvider.Steps),
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