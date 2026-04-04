using System;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.DomainModeling.Abstractions;


namespace Mississippi.DomainModeling.Runtime;

/// <summary>
///     Exposes typed runtime-status and manual-resume operations for saga-generated public surfaces.
/// </summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
internal sealed class SagaRecoveryService<TSaga> : ISagaRecoveryService<TSaga>
    where TSaga : class, ISagaState
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SagaRecoveryService{TSaga}" /> class.
    /// </summary>
    /// <param name="aggregateGrainFactory">Factory for loading the current saga state.</param>
    /// <param name="checkpointAccessor">Accessor for the latest authoritative recovery checkpoint.</param>
    /// <param name="coordinator">Coordinator for planning and applying manual resume requests.</param>
    /// <param name="recoveryInfoProvider">Provider for the configured saga recovery metadata.</param>
    /// <param name="sagaAccessAuthorizer">Authorizer for saga reads and manual resume operations.</param>
    /// <param name="sagaAccessContextProvider">Provider for the current caller-context fingerprint.</param>
    /// <param name="stepInfoProvider">Provider for the ordered saga step metadata.</param>
    /// <param name="timeProvider">Time provider used to stamp manual resume responses.</param>
    public SagaRecoveryService(
        IAggregateGrainFactory aggregateGrainFactory,
        SagaRecoveryCheckpointAccessor<TSaga> checkpointAccessor,
        SagaRecoveryCoordinator<TSaga> coordinator,
        ISagaRecoveryInfoProvider<TSaga> recoveryInfoProvider,
        ISagaAccessAuthorizer sagaAccessAuthorizer,
        ISagaAccessContextProvider sagaAccessContextProvider,
        ISagaStepInfoProvider<TSaga> stepInfoProvider,
        TimeProvider timeProvider
    )
    {
        ArgumentNullException.ThrowIfNull(aggregateGrainFactory);
        ArgumentNullException.ThrowIfNull(checkpointAccessor);
        ArgumentNullException.ThrowIfNull(coordinator);
        ArgumentNullException.ThrowIfNull(recoveryInfoProvider);
        ArgumentNullException.ThrowIfNull(sagaAccessAuthorizer);
        ArgumentNullException.ThrowIfNull(sagaAccessContextProvider);
        ArgumentNullException.ThrowIfNull(stepInfoProvider);
        ArgumentNullException.ThrowIfNull(timeProvider);
        AggregateGrainFactory = aggregateGrainFactory;
        CheckpointAccessor = checkpointAccessor;
        Coordinator = coordinator;
        RecoveryInfoProvider = recoveryInfoProvider;
        SagaAccessAuthorizer = sagaAccessAuthorizer;
        SagaAccessContextProvider = sagaAccessContextProvider;
        StepInfoProvider = stepInfoProvider;
        TimeProvider = timeProvider;
    }

    private IAggregateGrainFactory AggregateGrainFactory { get; }

    private SagaRecoveryCheckpointAccessor<TSaga> CheckpointAccessor { get; }

    private SagaRecoveryCoordinator<TSaga> Coordinator { get; }

    private ISagaAccessAuthorizer SagaAccessAuthorizer { get; }

    private ISagaAccessContextProvider SagaAccessContextProvider { get; }

    private ISagaRecoveryInfoProvider<TSaga> RecoveryInfoProvider { get; }

    private ISagaStepInfoProvider<TSaga> StepInfoProvider { get; }

    private TimeProvider TimeProvider { get; }

    /// <inheritdoc />
    public async Task<TSaga?> GetStateAsync(
        string entityId,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);
        (TSaga? state, SagaRecoveryCheckpoint? checkpoint) = await LoadStateAndCheckpointAsync(entityId, cancellationToken);
        if (state is null)
        {
            return null;
        }

        return Authorize(entityId, SagaAccessAction.ReadState, checkpoint).IsAuthorized
            ? state
            : null;
    }

    /// <inheritdoc />
    public async Task<SagaRuntimeStatus?> GetRuntimeStatusAsync(
        string entityId,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);
        (TSaga? state, SagaRecoveryCheckpoint? checkpoint) = await LoadStateAndCheckpointAsync(entityId, cancellationToken);
        if (state is null)
        {
            return null;
        }

        if (!Authorize(entityId, SagaAccessAction.ReadRuntimeStatus, checkpoint).IsAuthorized)
        {
            return null;
        }

        SagaRecoveryMode recoveryMode = checkpoint?.RecoveryMode ?? RecoveryInfoProvider.Recovery.Mode;
        bool workflowHashMatches = state.Phase is SagaPhase.NotStarted || HasMatchingWorkflowHash(state, checkpoint);
        return new SagaRuntimeStatus
        {
            AutomaticAttemptCount = checkpoint?.AutomaticAttemptCount ?? 0,
            BlockedReason = checkpoint?.BlockedReason,
            LastResumeAttemptedAt = checkpoint?.LastResumeAttemptedAt,
            LastResumeSource = checkpoint?.LastResumeSource,
            NextEligibleResumeAt = checkpoint?.NextEligibleResumeAt,
            PendingDirection = checkpoint?.PendingDirection,
            PendingStepIndex = checkpoint?.PendingStepIndex,
            PendingStepName = checkpoint?.PendingStepName,
            Phase = state.Phase.ToString(),
            RecoveryMode = recoveryMode,
            ReminderArmed = checkpoint?.ReminderArmed ?? false,
            ResumeDisposition = DetermineStatusDisposition(state, checkpoint, workflowHashMatches, recoveryMode),
            SagaId = state.SagaId,
            WorkflowHashMatches = workflowHashMatches,
        };
    }

    /// <inheritdoc />
    public async Task<SagaResumeResponse?> ResumeAsync(
        string entityId,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);
        (TSaga? state, SagaRecoveryCheckpoint? checkpoint) = await LoadStateAndCheckpointAsync(entityId, cancellationToken);
        if (state is null)
        {
            return null;
        }

        DateTimeOffset processedAt = TimeProvider.GetUtcNow();
        SagaAccessAuthorizationResult authorization = Authorize(entityId, SagaAccessAction.Resume, checkpoint);
        if (!authorization.IsAuthorized)
        {
            return new SagaResumeResponse
            {
                Disposition = SagaResumeRequestDisposition.Unauthorized,
                Message = authorization.FailureReason ?? "The current caller is not authorized to resume this saga.",
                ProcessedAt = processedAt,
                SagaId = state.SagaId,
                Source = SagaResumeSource.Manual,
            };
        }

        SagaRecoveryPlan plan = Coordinator.Plan(state, checkpoint, SagaResumeSource.Manual);
        if (plan.Disposition is SagaRecoveryPlanDisposition.ExecuteStep
            or SagaRecoveryPlanDisposition.Blocked
            or SagaRecoveryPlanDisposition.CompleteSaga
            or SagaRecoveryPlanDisposition.CompensateSaga)
        {
            ArgumentNullException.ThrowIfNull(checkpoint);
            OperationResult commandResult = await AggregateGrainFactory.GetGenericAggregate<TSaga>(entityId)
                .ExecuteAsync(
                    SagaRecoveryCoordinator<TSaga>.CreateResumeCommand(
                        plan,
                        checkpoint,
                        SagaResumeSource.Manual,
                        checkpoint.AccessContextFingerprint ?? SagaAccessContextProvider.GetFingerprint()),
                    cancellationToken);
            if (!commandResult.Success)
            {
                return CreateResumeResponse(state.SagaId, plan, processedAt, SagaResumeRequestDisposition.NoAction, commandResult.ErrorMessage);
            }
        }

        return CreateResumeResponse(state.SagaId, plan, processedAt, MapRequestDisposition(plan.Disposition), plan.Reason);
    }

    private static SagaResumeResponse CreateResumeResponse(
        Guid sagaId,
        SagaRecoveryPlan plan,
        DateTimeOffset processedAt,
        SagaResumeRequestDisposition disposition,
        string? message
    ) => new()
    {
        BlockedReason = plan.Disposition is SagaRecoveryPlanDisposition.Blocked ? plan.Reason : null,
        Disposition = disposition,
        Message = message ?? BuildAcceptedMessage(plan),
        PendingStepIndex = plan.Step?.StepIndex,
        PendingStepName = plan.Step?.StepName,
        ProcessedAt = processedAt,
        SagaId = sagaId,
        Source = SagaResumeSource.Manual,
    };

    private static string? BuildAcceptedMessage(
        SagaRecoveryPlan plan
    ) => plan.Disposition switch
    {
        SagaRecoveryPlanDisposition.ExecuteStep => "Manual resume accepted.",
        SagaRecoveryPlanDisposition.CompleteSaga => "Manual resume completed the saga.",
        SagaRecoveryPlanDisposition.CompensateSaga => "Manual resume completed saga compensation.",
        _ => null,
    };

    private static SagaResumeDisposition DetermineStatusDisposition(
        TSaga state,
        SagaRecoveryCheckpoint? checkpoint,
        bool workflowHashMatches,
        SagaRecoveryMode recoveryMode
    )
    {
        if (state.Phase is SagaPhase.Completed or SagaPhase.Compensated or SagaPhase.Failed)
        {
            return SagaResumeDisposition.Terminal;
        }

        if (!workflowHashMatches)
        {
            return SagaResumeDisposition.WorkflowMismatch;
        }

        if (!string.IsNullOrWhiteSpace(checkpoint?.BlockedReason))
        {
            return SagaResumeDisposition.ManualInterventionRequired;
        }

        if (checkpoint?.PendingDirection is not null)
        {
            return recoveryMode is SagaRecoveryMode.ManualOnly
                ? SagaResumeDisposition.ManualInterventionRequired
                : SagaResumeDisposition.AutomaticPending;
        }

        return SagaResumeDisposition.Idle;
    }

    private bool HasMatchingWorkflowHash(
        TSaga state,
        SagaRecoveryCheckpoint? checkpoint
    )
    {
        string expectedHash = SagaStepHash.Compute(RecoveryInfoProvider.Recovery, StepInfoProvider.Steps);
        string? actualHash = string.IsNullOrWhiteSpace(checkpoint?.StepHash)
            ? state.StepHash
            : checkpoint.StepHash;
        return !string.IsNullOrWhiteSpace(actualHash)
               && string.Equals(actualHash, expectedHash, StringComparison.Ordinal);
    }

    private static SagaResumeRequestDisposition MapRequestDisposition(
        SagaRecoveryPlanDisposition disposition
    ) => disposition switch
    {
        SagaRecoveryPlanDisposition.ExecuteStep or SagaRecoveryPlanDisposition.CompleteSaga
            or SagaRecoveryPlanDisposition.CompensateSaga => SagaResumeRequestDisposition.Accepted,
        SagaRecoveryPlanDisposition.Blocked => SagaResumeRequestDisposition.Blocked,
        SagaRecoveryPlanDisposition.Terminal => SagaResumeRequestDisposition.Terminal,
        SagaRecoveryPlanDisposition.WorkflowMismatch => SagaResumeRequestDisposition.WorkflowMismatch,
        _ => SagaResumeRequestDisposition.NoAction,
    };

    private SagaAccessAuthorizationResult Authorize(
        string entityId,
        SagaAccessAction action,
        SagaRecoveryCheckpoint? checkpoint
    ) =>
        SagaAccessAuthorizer.Authorize(
            entityId,
            action,
            checkpoint?.AccessContextFingerprint,
            SagaAccessContextProvider.GetFingerprint());

    private async Task<(TSaga? State, SagaRecoveryCheckpoint? Checkpoint)> LoadStateAndCheckpointAsync(
        string entityId,
        CancellationToken cancellationToken
    )
    {
        IGenericAggregateGrain<TSaga> grain = AggregateGrainFactory.GetGenericAggregate<TSaga>(entityId);
        TSaga? state = await grain.GetStateAsync(cancellationToken);
        SagaRecoveryCheckpoint? checkpoint = state is null
            ? null
            : await CheckpointAccessor.GetAsync(entityId, cancellationToken);
        return (state, checkpoint);
    }
}