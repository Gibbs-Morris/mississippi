using System;
using System.Collections.Generic;

using Mississippi.DomainModeling.Abstractions;


namespace Mississippi.DomainModeling.Runtime;

/// <summary>
///     Applies a previously planned saga recovery action by emitting the required framework event.
/// </summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
internal sealed class ResumeSagaCommandHandler<TSaga> : CommandHandlerBase<ResumeSagaCommand, TSaga>
    where TSaga : class, ISagaState
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ResumeSagaCommandHandler{TSaga}" /> class.
    /// </summary>
    /// <param name="timeProvider">The time provider.</param>
    public ResumeSagaCommandHandler(
        TimeProvider timeProvider
    )
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        TimeProvider = timeProvider;
    }

    private TimeProvider TimeProvider { get; }

    /// <inheritdoc />
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        ResumeSagaCommand command,
        TSaga? state
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        if (state is null || state.Phase is SagaPhase.NotStarted)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidState,
                $"Saga '{typeof(TSaga).Name}' has not started.");
        }

        DateTimeOffset now = TimeProvider.GetUtcNow();

        return command.Disposition switch
        {
            SagaRecoveryPlanDisposition.NoAction or SagaRecoveryPlanDisposition.Terminal or SagaRecoveryPlanDisposition.WorkflowMismatch
                => OperationResult.Ok<IReadOnlyList<object>>([]),
            SagaRecoveryPlanDisposition.CompleteSaga
                => OperationResult.Ok<IReadOnlyList<object>>(
                [
                    new SagaCompleted
                    {
                        CompletedAt = now,
                    },
                ]),
            SagaRecoveryPlanDisposition.CompensateSaga
                => OperationResult.Ok<IReadOnlyList<object>>(
                [
                    new SagaCompensated
                    {
                        CompletedAt = now,
                    },
                ]),
            SagaRecoveryPlanDisposition.Blocked
                => BuildBlockedResult(command, now),
            SagaRecoveryPlanDisposition.ExecuteStep
                => BuildExecutionStartedResult(command, state, now),
            _ => OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                $"Unhandled recovery disposition '{command.Disposition}'."),
        };
    }

    private static OperationResult<IReadOnlyList<object>> BuildBlockedResult(
        ResumeSagaCommand command,
        DateTimeOffset blockedAt
    )
    {
        if (command.Direction is null || command.StepIndex is null || string.IsNullOrWhiteSpace(command.StepName)
            || string.IsNullOrWhiteSpace(command.BlockedReason))
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidState,
                "Blocked recovery commands must include direction, step index, step name, and blocked reason.");
        }

        return OperationResult.Ok<IReadOnlyList<object>>(
        [
            new SagaResumeBlocked
            {
                AccessContextFingerprint = command.AccessContextFingerprint,
                BlockedAt = blockedAt,
                BlockedReason = command.BlockedReason,
                Direction = command.Direction.Value,
                Source = command.Source,
                StepIndex = command.StepIndex.Value,
                StepName = command.StepName,
            },
        ]);
    }

    private static OperationResult<IReadOnlyList<object>> BuildExecutionStartedResult(
        ResumeSagaCommand command,
        TSaga state,
        DateTimeOffset startedAt
    )
    {
        if (command.Direction is null || command.StepIndex is null || string.IsNullOrWhiteSpace(command.StepName))
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidState,
                "Execution recovery commands must include direction, step index, and step name.");
        }

        Guid attemptId = command.AttemptId ?? Guid.NewGuid();
        string operationKey = command.OperationKey
                              ?? SagaStepOperationKey.Compute(
                                  state.SagaId,
                                  command.StepIndex.Value,
                                  command.Direction.Value);

        return OperationResult.Ok<IReadOnlyList<object>>(
        [
            new SagaStepExecutionStarted
            {
                AccessContextFingerprint = command.AccessContextFingerprint,
                AttemptId = attemptId,
                Direction = command.Direction.Value,
                OperationKey = operationKey,
                Source = command.Source,
                StartedAt = startedAt,
                StepIndex = command.StepIndex.Value,
                StepName = command.StepName,
            },
        ]);
    }
}