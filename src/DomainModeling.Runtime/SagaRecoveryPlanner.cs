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

    private static SagaRecoveryPlan CreateDirectionalPlan(
        SagaRecoveryPlanDisposition disposition,
        SagaExecutionDirection direction
    ) =>
        new()
        {
            Disposition = disposition,
            Direction = direction,
        };

    private static SagaRecoveryPlan CreateNoActionPlan() =>
        new()
        {
            Disposition = SagaRecoveryPlanDisposition.NoAction,
        };

    private static SagaRecoveryPlan CreateWorkflowMismatchPlan(
        SagaExecutionDirection direction,
        string reason
    ) =>
        new()
        {
            Disposition = SagaRecoveryPlanDisposition.WorkflowMismatch,
            Direction = direction,
            Reason = reason,
        };

    private static bool IsReminderResumeBlocked(
        SagaResumeSource source,
        SagaRecoveryCheckpoint checkpoint,
        SagaRecoveryOptions recoveryOptions
    ) =>
        source is SagaResumeSource.Reminder &&
        (checkpoint.RecoveryMode is SagaRecoveryMode.ManualOnly ||
         !recoveryOptions.Enabled ||
         recoveryOptions.ForceManualOnly);

    private static bool TryCreateBoundaryPlan(
        SagaExecutionDirection direction,
        int pendingStepIndex,
        int stepCount,
        out SagaRecoveryPlan? plan
    )
    {
        if (direction is SagaExecutionDirection.Forward && (pendingStepIndex >= stepCount))
        {
            plan = CreateDirectionalPlan(SagaRecoveryPlanDisposition.CompleteSaga, SagaExecutionDirection.Forward);
            return true;
        }

        if (direction is SagaExecutionDirection.Compensation && (pendingStepIndex < 0))
        {
            plan = CreateDirectionalPlan(
                SagaRecoveryPlanDisposition.CompensateSaga,
                SagaExecutionDirection.Compensation);
            return true;
        }

        plan = null;
        return false;
    }

    private static bool TryCreateReminderBlockedPlan(
        SagaResumeSource source,
        SagaExecutionDirection direction,
        SagaStepInfo step,
        out SagaRecoveryPlan? plan
    )
    {
        if (source is not SagaResumeSource.Reminder)
        {
            plan = null;
            return false;
        }

        SagaStepRecoveryPolicy recoveryPolicy;
        if (direction is SagaExecutionDirection.Forward)
        {
            recoveryPolicy = step.ForwardRecoveryPolicy;
        }
        else
        {
            if (!step.HasCompensation)
            {
                plan = null;
                return false;
            }

            recoveryPolicy = step.CompensationRecoveryPolicy!.Value;
        }

        if (recoveryPolicy is not SagaStepRecoveryPolicy.ManualOnly)
        {
            plan = null;
            return false;
        }

        string directionName = direction is SagaExecutionDirection.Forward ? "forward" : "compensation";
        plan = new()
        {
            Disposition = SagaRecoveryPlanDisposition.Blocked,
            Direction = direction,
            Reason = $"Step '{step.StepName}' requires manual {directionName} recovery.",
            Step = step,
        };
        return true;
    }

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

        if (IsReminderResumeBlocked(source, checkpoint, RecoveryOptions))
        {
            return CreateNoActionPlan();
        }

        if (checkpoint.PendingDirection is null)
        {
            return CreateNoActionPlan();
        }

        SagaExecutionDirection direction = checkpoint.PendingDirection.Value;
        if (!checkpoint.PendingStepIndex.HasValue)
        {
            return CreateWorkflowMismatchPlan(direction, "Recovery checkpoint is missing a pending step index.");
        }

        int pendingStepIndex = checkpoint.PendingStepIndex.Value;
        if (HasWorkflowHashMismatch(state, checkpoint))
        {
            return CreateWorkflowMismatchPlan(direction, "Workflow hash mismatch prevents automatic resume.");
        }

        if (TryCreateBoundaryPlan(
                direction,
                pendingStepIndex,
                StepInfoProvider.Steps.Count,
                out SagaRecoveryPlan? plan))
        {
            return plan!;
        }

        if (TryCreatePendingStepPlan(direction, pendingStepIndex, out SagaStepInfo? step, out plan))
        {
            return plan!;
        }

        if (TryCreateReminderBlockedPlan(source, direction, step!, out plan))
        {
            return plan!;
        }

        return new()
        {
            Disposition = SagaRecoveryPlanDisposition.ExecuteStep,
            Direction = direction,
            Step = step,
        };
    }

    private bool HasWorkflowHashMismatch(
        TSaga state,
        SagaRecoveryCheckpoint checkpoint
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(checkpoint);
        string expectedHash = SagaStepHash.Compute(RecoveryInfoProvider.Recovery, StepInfoProvider.Steps);
        string? actualHash = string.IsNullOrWhiteSpace(checkpoint.StepHash) ? state.StepHash : checkpoint.StepHash;
        return string.IsNullOrWhiteSpace(actualHash) ||
               !string.Equals(actualHash, expectedHash, StringComparison.Ordinal);
    }

    private bool TryCreatePendingStepPlan(
        SagaExecutionDirection direction,
        int pendingStepIndex,
        out SagaStepInfo? step,
        out SagaRecoveryPlan? plan
    )
    {
        step = StepInfoProvider.Steps.FirstOrDefault(candidate => candidate.StepIndex == pendingStepIndex);
        if (step is null)
        {
            plan = CreateWorkflowMismatchPlan(
                direction,
                $"Pending step index '{pendingStepIndex}' is not registered in the current saga definition.");
            return true;
        }

        if (direction is SagaExecutionDirection.Compensation &&
            step.HasCompensation &&
            step.CompensationRecoveryPolicy is null)
        {
            plan = CreateWorkflowMismatchPlan(
                SagaExecutionDirection.Compensation,
                $"Step '{step.StepName}' no longer supports compensation recovery.");
            return true;
        }

        plan = null;
        return false;
    }
}