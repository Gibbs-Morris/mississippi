using System;
using System.Linq;

using Microsoft.Extensions.Options;

using Mississippi.DomainModeling.Abstractions;


namespace Mississippi.DomainModeling.Runtime;

/// <summary>
///     Selects the next recovery action for a saga from its durable checkpoint metadata.
/// </summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
internal sealed class SagaRecoveryPlanner<TSaga>
    where TSaga : class, ISagaState
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SagaRecoveryPlanner{TSaga}" /> class.
    /// </summary>
    /// <param name="stepInfoProvider">The ordered saga step metadata provider.</param>
    /// <param name="recoveryInfoProvider">The saga-level recovery metadata provider.</param>
    /// <param name="recoveryOptions">The runtime recovery overrides that gate reminder-driven resumes.</param>
    public SagaRecoveryPlanner(
        ISagaStepInfoProvider<TSaga> stepInfoProvider,
        ISagaRecoveryInfoProvider<TSaga> recoveryInfoProvider,
        IOptions<SagaRecoveryOptions> recoveryOptions
    )
    {
        ArgumentNullException.ThrowIfNull(stepInfoProvider);
        ArgumentNullException.ThrowIfNull(recoveryInfoProvider);
        ArgumentNullException.ThrowIfNull(recoveryOptions);
        ArgumentNullException.ThrowIfNull(recoveryOptions.Value);
        StepInfoProvider = stepInfoProvider;
        RecoveryInfoProvider = recoveryInfoProvider;
        RecoveryOptions = recoveryOptions.Value;
    }

    private ISagaRecoveryInfoProvider<TSaga> RecoveryInfoProvider { get; }

    private SagaRecoveryOptions RecoveryOptions { get; }

    private ISagaStepInfoProvider<TSaga> StepInfoProvider { get; }

    /// <summary>
    ///     Computes the next recovery action for the supplied saga state and checkpoint.
    /// </summary>
    /// <param name="state">The current saga state.</param>
    /// <param name="checkpoint">The current recovery checkpoint.</param>
    /// <param name="source">The source requesting the recovery decision.</param>
    /// <returns>The selected recovery action.</returns>
    public SagaRecoveryPlan Plan(
        TSaga state,
        SagaRecoveryCheckpoint checkpoint,
        SagaResumeSource source
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(checkpoint);
        if (state.Phase is SagaPhase.Completed or SagaPhase.Compensated or SagaPhase.Failed)
        {
            return new()
            {
                Disposition = SagaRecoveryPlanDisposition.Terminal,
            };
        }

        if (source is SagaResumeSource.Reminder && checkpoint.RecoveryMode is SagaRecoveryMode.ManualOnly)
        {
            return new()
            {
                Disposition = SagaRecoveryPlanDisposition.NoAction,
            };
        }

        if (source is SagaResumeSource.Reminder && (!RecoveryOptions.Enabled || RecoveryOptions.ForceManualOnly))
        {
            return new()
            {
                Disposition = SagaRecoveryPlanDisposition.NoAction,
            };
        }

        if (checkpoint.PendingDirection is null)
        {
            return new()
            {
                Disposition = SagaRecoveryPlanDisposition.NoAction,
            };
        }

        if (!checkpoint.PendingStepIndex.HasValue)
        {
            return new()
            {
                Disposition = SagaRecoveryPlanDisposition.WorkflowMismatch,
                Direction = checkpoint.PendingDirection,
                Reason = "Recovery checkpoint is missing a pending step index.",
            };
        }

        int pendingStepIndex = checkpoint.PendingStepIndex.Value;
        string expectedHash = SagaStepHash.Compute(RecoveryInfoProvider.Recovery, StepInfoProvider.Steps);
        string? actualHash = string.IsNullOrWhiteSpace(checkpoint.StepHash) ? state.StepHash : checkpoint.StepHash;
        if (string.IsNullOrWhiteSpace(actualHash) || !string.Equals(actualHash, expectedHash, StringComparison.Ordinal))
        {
            return new()
            {
                Disposition = SagaRecoveryPlanDisposition.WorkflowMismatch,
                Direction = checkpoint.PendingDirection,
                Reason = "Workflow hash mismatch prevents automatic resume.",
            };
        }

        if (checkpoint.PendingDirection is SagaExecutionDirection.Forward &&
            (pendingStepIndex >= StepInfoProvider.Steps.Count))
        {
            return new()
            {
                Disposition = SagaRecoveryPlanDisposition.CompleteSaga,
                Direction = SagaExecutionDirection.Forward,
            };
        }

        if (checkpoint.PendingDirection is SagaExecutionDirection.Compensation && (pendingStepIndex < 0))
        {
            return new()
            {
                Disposition = SagaRecoveryPlanDisposition.CompensateSaga,
                Direction = SagaExecutionDirection.Compensation,
            };
        }

        SagaStepInfo? step = StepInfoProvider.Steps.FirstOrDefault(s => s.StepIndex == pendingStepIndex);
        if (step is null)
        {
            return new()
            {
                Disposition = SagaRecoveryPlanDisposition.WorkflowMismatch,
                Direction = checkpoint.PendingDirection,
                Reason = $"Pending step index '{pendingStepIndex}' is not registered in the current saga definition.",
            };
        }

        if (checkpoint.PendingDirection is SagaExecutionDirection.Compensation &&
            step.CompensationRecoveryPolicy is null)
        {
            return new()
            {
                Disposition = SagaRecoveryPlanDisposition.WorkflowMismatch,
                Direction = SagaExecutionDirection.Compensation,
                Reason = $"Step '{step.StepName}' no longer supports compensation recovery.",
            };
        }

        if (source is SagaResumeSource.Reminder)
        {
            SagaStepRecoveryPolicy recoveryPolicy = checkpoint.PendingDirection is SagaExecutionDirection.Forward
                ? step.ForwardRecoveryPolicy
                : step.CompensationRecoveryPolicy!.Value;
            if (recoveryPolicy is SagaStepRecoveryPolicy.ManualOnly)
            {
                string directionName = checkpoint.PendingDirection is SagaExecutionDirection.Forward
                    ? "forward"
                    : "compensation";
                return new()
                {
                    Disposition = SagaRecoveryPlanDisposition.Blocked,
                    Direction = checkpoint.PendingDirection,
                    Reason = $"Step '{step.StepName}' requires manual {directionName} recovery.",
                    Step = step,
                };
            }
        }

        return new()
        {
            Disposition = SagaRecoveryPlanDisposition.ExecuteStep,
            Direction = checkpoint.PendingDirection,
            Step = step,
        };
    }
}