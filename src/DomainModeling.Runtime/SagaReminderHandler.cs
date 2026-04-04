using System;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.Brooks.Abstractions;
using Mississippi.DomainModeling.Abstractions;

using Orleans.Runtime;


namespace Mississippi.DomainModeling.Runtime;

/// <summary>
///     Handles saga recovery reminders by reusing the runtime planner and resume command path.
/// </summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
internal sealed class SagaReminderHandler<TSaga> : IAggregateReminderHandler<TSaga>
    where TSaga : class, ISagaState
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SagaReminderHandler{TSaga}" /> class.
    /// </summary>
    /// <param name="checkpointAccessor">Accessor for the authoritative saga recovery checkpoint.</param>
    /// <param name="recoveryCoordinator">Coordinator that plans and materializes recovery work.</param>
    public SagaReminderHandler(
        SagaRecoveryCheckpointAccessor<TSaga> checkpointAccessor,
        SagaRecoveryCoordinator<TSaga> recoveryCoordinator
    )
    {
        ArgumentNullException.ThrowIfNull(checkpointAccessor);
        ArgumentNullException.ThrowIfNull(recoveryCoordinator);
        CheckpointAccessor = checkpointAccessor;
        RecoveryCoordinator = recoveryCoordinator;
    }

    private SagaRecoveryCheckpointAccessor<TSaga> CheckpointAccessor { get; }

    private SagaRecoveryCoordinator<TSaga> RecoveryCoordinator { get; }

    /// <inheritdoc />
    public async Task<bool> ReceiveReminderAsync(
        string entityId,
        string reminderName,
        TickStatus status,
        Func<CancellationToken, Task<TSaga?>> loadStateAsync,
        Func<object, CancellationToken, Task<OperationResult>> executeCommandAsync,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);
        ArgumentException.ThrowIfNullOrWhiteSpace(reminderName);
        ArgumentNullException.ThrowIfNull(loadStateAsync);
        ArgumentNullException.ThrowIfNull(executeCommandAsync);

        _ = status;

        if (!string.Equals(reminderName, SagaReminderNames.Recovery, StringComparison.Ordinal))
        {
            return false;
        }

        TSaga? state = await loadStateAsync(cancellationToken);
        SagaRecoveryCheckpoint? checkpoint = state is null
            || state.Phase is SagaPhase.Completed or SagaPhase.Compensated or SagaPhase.Failed
            ? null
            : await CheckpointAccessor.GetAsync(entityId, cancellationToken);
        SagaRecoveryPlan plan = RecoveryCoordinator.Plan(state, checkpoint, SagaResumeSource.Reminder);
        if (plan.Disposition is SagaRecoveryPlanDisposition.WorkflowMismatch)
        {
            if (checkpoint?.PendingDirection is not null && checkpoint.PendingStepIndex.HasValue)
            {
                await executeCommandAsync(
                    new ResumeSagaCommand
                    {
                        BlockedReason = plan.Reason ?? "Workflow hash mismatch prevents automatic resume.",
                        Direction = checkpoint.PendingDirection,
                        Disposition = SagaRecoveryPlanDisposition.Blocked,
                        Source = SagaResumeSource.Reminder,
                        StepIndex = checkpoint.PendingStepIndex.Value,
                        StepName = string.IsNullOrWhiteSpace(checkpoint.PendingStepName)
                            ? $"Step {checkpoint.PendingStepIndex.Value}"
                            : checkpoint.PendingStepName,
                    },
                    cancellationToken);
            }

            return true;
        }

        if (plan.Disposition is SagaRecoveryPlanDisposition.NoAction
            or SagaRecoveryPlanDisposition.Terminal)
        {
            return true;
        }

        await executeCommandAsync(
            SagaRecoveryCoordinator<TSaga>.CreateResumeCommand(plan, checkpoint!, SagaResumeSource.Reminder),
            cancellationToken);
        return true;
    }
}