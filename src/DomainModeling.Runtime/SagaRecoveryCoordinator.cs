using System;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.DomainModeling.Abstractions;


namespace Mississippi.DomainModeling.Runtime;

/// <summary>
///     Loads saga recovery inputs and delegates resume planning to the runtime planner.
/// </summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
internal sealed class SagaRecoveryCoordinator<TSaga>
    where TSaga : class, ISagaState
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SagaRecoveryCoordinator{TSaga}" /> class.
    /// </summary>
    /// <param name="aggregateGrainFactory">Factory for loading the current saga state.</param>
    /// <param name="checkpointAccessor">Accessor for the latest authoritative recovery checkpoint.</param>
    /// <param name="planner">Planner that converts loaded recovery state into a recovery decision.</param>
    public SagaRecoveryCoordinator(
        IAggregateGrainFactory aggregateGrainFactory,
        SagaRecoveryCheckpointAccessor<TSaga> checkpointAccessor,
        SagaRecoveryPlanner<TSaga> planner
    )
    {
        ArgumentNullException.ThrowIfNull(aggregateGrainFactory);
        ArgumentNullException.ThrowIfNull(checkpointAccessor);
        ArgumentNullException.ThrowIfNull(planner);
        AggregateGrainFactory = aggregateGrainFactory;
        CheckpointAccessor = checkpointAccessor;
        Planner = planner;
    }

    private IAggregateGrainFactory AggregateGrainFactory { get; }

    private SagaRecoveryCheckpointAccessor<TSaga> CheckpointAccessor { get; }

    private SagaRecoveryPlanner<TSaga> Planner { get; }

    /// <summary>
    ///     Converts an actionable recovery plan into the internal resume command payload.
    /// </summary>
    /// <param name="plan">The recovery plan selected for execution.</param>
    /// <param name="checkpoint">The authoritative checkpoint that supplies in-flight metadata.</param>
    /// <param name="source">The source requesting resume execution.</param>
    /// <param name="accessContextFingerprint">The optional caller-context fingerprint for explicit resumes.</param>
    /// <returns>The command that materializes the selected recovery action.</returns>
    internal static ResumeSagaCommand CreateResumeCommand(
        SagaRecoveryPlan plan,
        SagaRecoveryCheckpoint checkpoint,
        SagaResumeSource source,
        string? accessContextFingerprint = null
    )
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(checkpoint);
        if (plan.Disposition is SagaRecoveryPlanDisposition.ExecuteStep)
        {
            ArgumentNullException.ThrowIfNull(plan.Step);
            return new()
            {
                AccessContextFingerprint = accessContextFingerprint,
                AttemptId = checkpoint.InFlightAttemptId,
                Direction = plan.Direction,
                Disposition = plan.Disposition,
                OperationKey = checkpoint.InFlightOperationKey,
                Source = source,
                StepIndex = plan.Step.StepIndex,
                StepName = plan.Step.StepName,
            };
        }

        if (plan.Disposition is SagaRecoveryPlanDisposition.Blocked)
        {
            ArgumentNullException.ThrowIfNull(plan.Step);
            return new()
            {
                AccessContextFingerprint = accessContextFingerprint,
                BlockedReason = plan.Reason,
                Direction = plan.Direction,
                Disposition = plan.Disposition,
                Source = source,
                StepIndex = plan.Step.StepIndex,
                StepName = plan.Step.StepName,
            };
        }

        return new()
        {
            AccessContextFingerprint = accessContextFingerprint,
            Disposition = plan.Disposition,
            Source = source,
        };
    }

    /// <summary>
    ///     Loads the current saga state and checkpoint, then computes the next recovery action.
    /// </summary>
    /// <param name="entityId">The saga entity identifier.</param>
    /// <param name="source">The source requesting recovery planning.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The recovery plan selected for the current saga instance.</returns>
    public async Task<SagaRecoveryPlan> PlanAsync(
        string entityId,
        SagaResumeSource source,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);
        (IGenericAggregateGrain<TSaga> _, TSaga? state, SagaRecoveryCheckpoint? checkpoint) =
            await LoadInputsAsync(entityId, cancellationToken);
        if (state is null)
        {
            return new()
            {
                Disposition = SagaRecoveryPlanDisposition.NoAction,
                Reason = "Saga state not found.",
            };
        }

        if (state.Phase is SagaPhase.Completed or SagaPhase.Compensated or SagaPhase.Failed)
        {
            return new()
            {
                Disposition = SagaRecoveryPlanDisposition.Terminal,
            };
        }

        if (checkpoint is null)
        {
            return new()
            {
                Disposition = SagaRecoveryPlanDisposition.NoAction,
                Reason = "Recovery checkpoint not found.",
            };
        }

        return Plan(state, checkpoint, source);
    }

    /// <summary>
    ///     Applies the planned recovery action for the specified saga when work needs to execute.
    /// </summary>
    /// <param name="entityId">The saga entity identifier.</param>
    /// <param name="source">The source requesting recovery execution.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The selected recovery plan when command execution succeeds.</returns>
    public async Task<OperationResult<SagaRecoveryPlan>> ResumeAsync(
        string entityId,
        SagaResumeSource source,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);
        (IGenericAggregateGrain<TSaga> grain, TSaga? state, SagaRecoveryCheckpoint? checkpoint) =
            await LoadInputsAsync(entityId, cancellationToken);
        if (state is null)
        {
            return OperationResult.Ok(
                new SagaRecoveryPlan
                {
                    Disposition = SagaRecoveryPlanDisposition.NoAction,
                    Reason = "Saga state not found.",
                });
        }

        if (state.Phase is SagaPhase.Completed or SagaPhase.Compensated or SagaPhase.Failed)
        {
            return OperationResult.Ok(
                new SagaRecoveryPlan
                {
                    Disposition = SagaRecoveryPlanDisposition.Terminal,
                });
        }

        if (checkpoint is null)
        {
            return OperationResult.Ok(
                new SagaRecoveryPlan
                {
                    Disposition = SagaRecoveryPlanDisposition.NoAction,
                    Reason = "Recovery checkpoint not found.",
                });
        }

        SagaRecoveryPlan plan = Plan(state, checkpoint, source);
        if (plan.Disposition is SagaRecoveryPlanDisposition.NoAction or SagaRecoveryPlanDisposition.WorkflowMismatch)
        {
            return OperationResult.Ok(plan);
        }

        OperationResult commandResult = await grain.ExecuteAsync(
            CreateResumeCommand(plan, checkpoint, source),
            cancellationToken);
        if (!commandResult.Success)
        {
            return OperationResult.Fail<SagaRecoveryPlan>(commandResult.ErrorCode, commandResult.ErrorMessage);
        }

        return OperationResult.Ok(plan);
    }

    /// <summary>
    ///     Computes the effective recovery plan for already-loaded saga state and checkpoint inputs.
    /// </summary>
    /// <param name="state">The current saga state, if any.</param>
    /// <param name="checkpoint">The current authoritative recovery checkpoint, if any.</param>
    /// <param name="source">The source requesting resume evaluation.</param>
    /// <returns>The selected recovery plan for the supplied runtime state.</returns>
    internal SagaRecoveryPlan Plan(
        TSaga? state,
        SagaRecoveryCheckpoint? checkpoint,
        SagaResumeSource source
    )
    {
        if (state is null)
        {
            return new()
            {
                Disposition = SagaRecoveryPlanDisposition.NoAction,
                Reason = "Saga state not found.",
            };
        }

        if (state.Phase is SagaPhase.Completed or SagaPhase.Compensated or SagaPhase.Failed)
        {
            return new()
            {
                Disposition = SagaRecoveryPlanDisposition.Terminal,
            };
        }

        if (checkpoint is null)
        {
            return new()
            {
                Disposition = SagaRecoveryPlanDisposition.NoAction,
                Reason = "Recovery checkpoint not found.",
            };
        }

        return Planner.Plan(state, checkpoint, source);
    }

    private async Task<(IGenericAggregateGrain<TSaga> Grain, TSaga? State, SagaRecoveryCheckpoint? Checkpoint)>
        LoadInputsAsync(
            string entityId,
            CancellationToken cancellationToken
        )
    {
        IGenericAggregateGrain<TSaga> grain = AggregateGrainFactory.GetGenericAggregate<TSaga>(entityId);
        TSaga? state = await grain.GetStateAsync(cancellationToken);
        SagaRecoveryCheckpoint? checkpoint =
            state is null || state.Phase is SagaPhase.Completed or SagaPhase.Compensated or SagaPhase.Failed
                ? null
                : await CheckpointAccessor.GetAsync(entityId, cancellationToken);
        return (grain, state, checkpoint);
    }
}