using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

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
    /// <param name="stepInfoProvider">The saga step metadata provider.</param>
    /// <param name="recoveryInfoProvider">The saga recovery metadata provider.</param>
    /// <param name="timeProvider">The time provider.</param>
    public StartSagaCommandHandler(
        ISagaStepInfoProvider<TSaga> stepInfoProvider,
        ISagaRecoveryInfoProvider<TSaga> recoveryInfoProvider,
        TimeProvider timeProvider
    )
    {
        ArgumentNullException.ThrowIfNull(stepInfoProvider);
        ArgumentNullException.ThrowIfNull(recoveryInfoProvider);
        ArgumentNullException.ThrowIfNull(timeProvider);
        StepInfoProvider = stepInfoProvider;
        RecoveryInfoProvider = recoveryInfoProvider;
        TimeProvider = timeProvider;
    }

    private ISagaRecoveryInfoProvider<TSaga> RecoveryInfoProvider { get; }

    private ISagaStepInfoProvider<TSaga> StepInfoProvider { get; }

    private TimeProvider TimeProvider { get; }

    private static string ComputeStepHash(
        SagaRecoveryInfo recovery,
        IReadOnlyList<SagaStepInfo> steps
    )
    {
        ArgumentNullException.ThrowIfNull(recovery);
        ArgumentNullException.ThrowIfNull(steps);
        StringBuilder builder = new();
        builder.Append("Recovery=")
            .Append(recovery.Mode)
            .Append(':')
            .Append(recovery.Profile ?? "NONE");
        for (int i = 0; i < steps.Count; i++)
        {
            SagaStepInfo step = steps[i];
            builder.Append('|');

            string stepTypeName = step.StepType.FullName ?? step.StepType.Name;
            builder.Append(step.StepIndex)
                .Append(':')
                .Append(step.StepName)
                .Append(':')
                .Append(stepTypeName)
                .Append(':')
                .Append(step.HasCompensation)
                .Append(':')
                .Append(step.ForwardRecoveryPolicy)
                .Append(':')
                .Append(step.CompensationRecoveryPolicy?.ToString() ?? "NONE");
        }

        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexString(bytes);
    }

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
            SagaId = command.SagaId,
            StepHash = ComputeStepHash(RecoveryInfoProvider.Recovery, StepInfoProvider.Steps),
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