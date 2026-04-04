using System.Collections.Generic;

using Microsoft.Extensions.Options;

using Mississippi.DomainModeling.Abstractions;


namespace Mississippi.DomainModeling.Runtime.L0Tests;

/// <summary>
///     Tests for <see cref="SagaRecoveryPlanner{TSaga}" />.
/// </summary>
public sealed class SagaRecoveryPlannerTests
{
    private static readonly IReadOnlyList<SagaStepInfo> CompensationWithNoOpStep =
    [
        new(0, "Validate", typeof(SagaNonCompensatableStep), false, SagaStepRecoveryPolicy.Automatic, null),
        new(
            1,
            "Debit",
            typeof(SagaSuccessStep),
            true,
            SagaStepRecoveryPolicy.Automatic,
            SagaStepRecoveryPolicy.Automatic),
    ];

    private static readonly IReadOnlyList<SagaStepInfo> ForwardThenCompensatingSteps =
    [
        new(
            0,
            "Debit",
            typeof(SagaSuccessStep),
            true,
            SagaStepRecoveryPolicy.Automatic,
            SagaStepRecoveryPolicy.ManualOnly),
        new(1, "Credit", typeof(SagaNonCompensatableStep), false, SagaStepRecoveryPolicy.ManualOnly, null),
    ];

    private static string ComputeHash(
        SagaRecoveryMode recoveryMode = SagaRecoveryMode.Automatic,
        IReadOnlyList<SagaStepInfo>? steps = null,
        string? profile = null
    ) =>
        SagaStepHash.Compute(new(recoveryMode, profile), steps ?? ForwardThenCompensatingSteps);

    private static SagaRecoveryPlanner<TestSagaState> CreatePlanner(
        SagaRecoveryMode recoveryMode = SagaRecoveryMode.Automatic,
        IReadOnlyList<SagaStepInfo>? steps = null,
        string? profile = null,
        SagaRecoveryOptions? recoveryOptions = null
    ) =>
        new(
            new SagaStepInfoProvider<TestSagaState>(steps ?? ForwardThenCompensatingSteps),
            new SagaRecoveryInfoProvider<TestSagaState>(new(recoveryMode, profile)),
            Options.Create(recoveryOptions ?? new SagaRecoveryOptions()));

    /// <summary>
    ///     Verifies reminder-driven manual-only compensation blocks instead of executing the step.
    /// </summary>
    [Fact]
    public void PlanBlocksReminderForManualOnlyCompensationStep()
    {
        SagaRecoveryPlanner<TestSagaState> planner = CreatePlanner();
        TestSagaState state = new()
        {
            Phase = SagaPhase.Compensating,
            StepHash = ComputeHash(),
        };
        SagaRecoveryCheckpoint checkpoint = new()
        {
            PendingDirection = SagaExecutionDirection.Compensation,
            PendingStepIndex = 0,
            StepHash = ComputeHash(),
        };
        SagaRecoveryPlan plan = planner.Plan(state, checkpoint, SagaResumeSource.Reminder);
        Assert.Equal(SagaRecoveryPlanDisposition.Blocked, plan.Disposition);
        Assert.Equal(SagaExecutionDirection.Compensation, plan.Direction);
        Assert.Equal("Step 'Debit' requires manual compensation recovery.", plan.Reason);
    }

    /// <summary>
    ///     Verifies reminder-driven manual-only forward recovery blocks instead of executing the step.
    /// </summary>
    [Fact]
    public void PlanBlocksReminderForManualOnlyForwardStep()
    {
        SagaRecoveryPlanner<TestSagaState> planner = CreatePlanner();
        TestSagaState state = new()
        {
            Phase = SagaPhase.Running,
            StepHash = ComputeHash(),
        };
        SagaRecoveryCheckpoint checkpoint = new()
        {
            PendingDirection = SagaExecutionDirection.Forward,
            PendingStepIndex = 1,
            StepHash = ComputeHash(),
        };
        SagaRecoveryPlan plan = planner.Plan(state, checkpoint, SagaResumeSource.Reminder);
        Assert.Equal(SagaRecoveryPlanDisposition.Blocked, plan.Disposition);
        Assert.Equal(SagaExecutionDirection.Forward, plan.Direction);
        Assert.Equal("Step 'Credit' requires manual forward recovery.", plan.Reason);
        Assert.NotNull(plan.Step);
        Assert.Equal(1, plan.Step.StepIndex);
    }

    /// <summary>
    ///     Verifies compensation checkpoints before the first step finalize the saga as compensated.
    /// </summary>
    [Fact]
    public void PlanCompensatesSagaWhenCompensationCursorIsBeforeFirstStep()
    {
        SagaRecoveryPlanner<TestSagaState> planner = CreatePlanner();
        TestSagaState state = new()
        {
            Phase = SagaPhase.Compensating,
            StepHash = ComputeHash(),
        };
        SagaRecoveryCheckpoint checkpoint = new()
        {
            PendingDirection = SagaExecutionDirection.Compensation,
            PendingStepIndex = -1,
            StepHash = ComputeHash(),
        };
        SagaRecoveryPlan plan = planner.Plan(state, checkpoint, SagaResumeSource.Reminder);
        Assert.Equal(SagaRecoveryPlanDisposition.CompensateSaga, plan.Disposition);
    }

    /// <summary>
    ///     Verifies forward checkpoints beyond the final step complete the saga instead of executing another step.
    /// </summary>
    [Fact]
    public void PlanCompletesSagaWhenForwardCursorIsPastFinalStep()
    {
        SagaRecoveryPlanner<TestSagaState> planner = CreatePlanner();
        TestSagaState state = new()
        {
            Phase = SagaPhase.Running,
            StepHash = ComputeHash(),
        };
        SagaRecoveryCheckpoint checkpoint = new()
        {
            PendingDirection = SagaExecutionDirection.Forward,
            PendingStepIndex = ForwardThenCompensatingSteps.Count,
            StepHash = ComputeHash(),
        };
        SagaRecoveryPlan plan = planner.Plan(state, checkpoint, SagaResumeSource.Reminder);
        Assert.Equal(SagaRecoveryPlanDisposition.CompleteSaga, plan.Disposition);
    }

    /// <summary>
    ///     Verifies manual resume can execute a manual-only forward step.
    /// </summary>
    [Fact]
    public void PlanExecutesManualOnlyForwardStepForManualResume()
    {
        SagaRecoveryPlanner<TestSagaState> planner = CreatePlanner();
        TestSagaState state = new()
        {
            Phase = SagaPhase.Running,
            StepHash = ComputeHash(),
        };
        SagaRecoveryCheckpoint checkpoint = new()
        {
            PendingDirection = SagaExecutionDirection.Forward,
            PendingStepIndex = 1,
            StepHash = ComputeHash(),
        };
        SagaRecoveryPlan plan = planner.Plan(state, checkpoint, SagaResumeSource.Manual);
        Assert.Equal(SagaRecoveryPlanDisposition.ExecuteStep, plan.Disposition);
        Assert.Equal(SagaExecutionDirection.Forward, plan.Direction);
        Assert.NotNull(plan.Step);
        Assert.Equal(1, plan.Step.StepIndex);
    }

    /// <summary>
    ///     Verifies recovery can continue through non-compensatable steps during compensation because the live
    ///     orchestration path treats them as no-op compensations.
    /// </summary>
    [Fact]
    public void PlanExecutesNonCompensatableStepDuringCompensationRecovery()
    {
        SagaRecoveryPlanner<TestSagaState> planner = CreatePlanner(steps: CompensationWithNoOpStep);
        TestSagaState state = new()
        {
            Phase = SagaPhase.Compensating,
            StepHash = ComputeHash(steps: CompensationWithNoOpStep),
        };
        SagaRecoveryCheckpoint checkpoint = new()
        {
            PendingDirection = SagaExecutionDirection.Compensation,
            PendingStepIndex = 0,
            StepHash = ComputeHash(steps: CompensationWithNoOpStep),
        };
        SagaRecoveryPlan plan = planner.Plan(state, checkpoint, SagaResumeSource.Reminder);
        Assert.Equal(SagaRecoveryPlanDisposition.ExecuteStep, plan.Disposition);
        Assert.Equal(SagaExecutionDirection.Compensation, plan.Direction);
        Assert.NotNull(plan.Step);
        Assert.Equal(0, plan.Step.StepIndex);
    }

    /// <summary>
    ///     Verifies stale reminder ticks do nothing when the saga is configured manual-only.
    /// </summary>
    [Fact]
    public void PlanReturnsNoActionForReminderAgainstManualOnlySaga()
    {
        SagaRecoveryPlanner<TestSagaState> planner = CreatePlanner(SagaRecoveryMode.ManualOnly);
        TestSagaState state = new()
        {
            Phase = SagaPhase.Running,
            StepHash = ComputeHash(SagaRecoveryMode.ManualOnly),
        };
        SagaRecoveryCheckpoint checkpoint = new()
        {
            RecoveryMode = SagaRecoveryMode.ManualOnly,
            PendingDirection = SagaExecutionDirection.Forward,
            PendingStepIndex = 0,
            StepHash = ComputeHash(SagaRecoveryMode.ManualOnly),
        };
        SagaRecoveryPlan plan = planner.Plan(state, checkpoint, SagaResumeSource.Reminder);
        Assert.Equal(SagaRecoveryPlanDisposition.NoAction, plan.Disposition);
    }

    /// <summary>
    ///     Verifies stale reminder ticks do nothing when automatic recovery is globally forced into manual-only mode.
    /// </summary>
    [Fact]
    public void PlanReturnsNoActionForReminderWhenAutomaticRecoveryForcedManual()
    {
        SagaRecoveryPlanner<TestSagaState> planner = CreatePlanner(
            recoveryOptions: new()
            {
                ForceManualOnly = true,
            });
        TestSagaState state = new()
        {
            Phase = SagaPhase.Running,
            StepHash = ComputeHash(),
        };
        SagaRecoveryCheckpoint checkpoint = new()
        {
            RecoveryMode = SagaRecoveryMode.Automatic,
            PendingDirection = SagaExecutionDirection.Forward,
            PendingStepIndex = 0,
            StepHash = ComputeHash(),
        };
        SagaRecoveryPlan plan = planner.Plan(state, checkpoint, SagaResumeSource.Reminder);
        Assert.Equal(SagaRecoveryPlanDisposition.NoAction, plan.Disposition);
    }

    /// <summary>
    ///     Verifies terminal sagas do not schedule additional recovery work.
    /// </summary>
    [Fact]
    public void PlanReturnsTerminalForTerminalSagaState()
    {
        SagaRecoveryPlanner<TestSagaState> planner = CreatePlanner();
        TestSagaState state = new()
        {
            Phase = SagaPhase.Completed,
            StepHash = ComputeHash(),
        };
        SagaRecoveryCheckpoint checkpoint = new()
        {
            PendingDirection = SagaExecutionDirection.Forward,
            PendingStepIndex = 0,
            StepHash = ComputeHash(),
        };
        SagaRecoveryPlan plan = planner.Plan(state, checkpoint, SagaResumeSource.Reminder);
        Assert.Equal(SagaRecoveryPlanDisposition.Terminal, plan.Disposition);
        Assert.Null(plan.Step);
    }

    /// <summary>
    ///     Verifies a checkpoint with a direction but no pending step index is treated as invalid recovery metadata.
    /// </summary>
    [Fact]
    public void PlanReturnsWorkflowMismatchWhenPendingStepIndexMissing()
    {
        SagaRecoveryPlanner<TestSagaState> planner = CreatePlanner();
        TestSagaState state = new()
        {
            Phase = SagaPhase.Running,
            StepHash = ComputeHash(),
        };
        SagaRecoveryCheckpoint checkpoint = new()
        {
            PendingDirection = SagaExecutionDirection.Forward,
            StepHash = ComputeHash(),
        };
        SagaRecoveryPlan plan = planner.Plan(state, checkpoint, SagaResumeSource.Reminder);
        Assert.Equal(SagaRecoveryPlanDisposition.WorkflowMismatch, plan.Disposition);
        Assert.Equal(SagaExecutionDirection.Forward, plan.Direction);
        Assert.Equal("Recovery checkpoint is missing a pending step index.", plan.Reason);
    }

    /// <summary>
    ///     Verifies workflow hash mismatches block automatic resume planning.
    /// </summary>
    [Fact]
    public void PlanReturnsWorkflowMismatchWhenStoredHashDiffers()
    {
        SagaRecoveryPlanner<TestSagaState> planner = CreatePlanner();
        TestSagaState state = new()
        {
            Phase = SagaPhase.Running,
            StepHash = "DIFFERENT",
        };
        SagaRecoveryCheckpoint checkpoint = new()
        {
            PendingDirection = SagaExecutionDirection.Forward,
            PendingStepIndex = 0,
            StepHash = "DIFFERENT",
        };
        SagaRecoveryPlan plan = planner.Plan(state, checkpoint, SagaResumeSource.Reminder);
        Assert.Equal(SagaRecoveryPlanDisposition.WorkflowMismatch, plan.Disposition);
        Assert.Equal("Workflow hash mismatch prevents automatic resume.", plan.Reason);
    }
}