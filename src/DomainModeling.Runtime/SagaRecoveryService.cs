using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Abstractions.Attributes;
using Mississippi.Brooks.Abstractions.Factory;
using Mississippi.DomainModeling.Abstractions;
using Mississippi.Tributary.Abstractions;


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
    /// <param name="brookGrainFactory">Factory for resolving brook cursor grains for recovery-checkpoint lookups.</param>
    /// <param name="snapshotGrainFactory">Factory for resolving checkpoint snapshot cache grains.</param>
    /// <param name="recoveryInfoProvider">Provider for the configured saga recovery metadata.</param>
    /// <param name="recoveryOptions">Runtime recovery overrides that shape effective reminder/readiness behavior.</param>
    /// <param name="sagaAccessAuthorizer">Authorizer for saga reads and manual resume operations.</param>
    /// <param name="sagaAccessContextProvider">Provider for the current caller-context fingerprint.</param>
    /// <param name="stepInfoProvider">Provider for the ordered saga step metadata.</param>
    /// <param name="timeProvider">Time provider used to stamp manual resume responses.</param>
    public SagaRecoveryService(
        IAggregateGrainFactory aggregateGrainFactory,
        IBrookGrainFactory brookGrainFactory,
        ISnapshotGrainFactory snapshotGrainFactory,
        ISagaRecoveryInfoProvider<TSaga> recoveryInfoProvider,
        IOptions<SagaRecoveryOptions> recoveryOptions,
        ISagaAccessAuthorizer sagaAccessAuthorizer,
        ISagaAccessContextProvider sagaAccessContextProvider,
        ISagaStepInfoProvider<TSaga> stepInfoProvider,
        TimeProvider timeProvider
    )
    {
        ArgumentNullException.ThrowIfNull(aggregateGrainFactory);
        ArgumentNullException.ThrowIfNull(brookGrainFactory);
        ArgumentNullException.ThrowIfNull(snapshotGrainFactory);
        ArgumentNullException.ThrowIfNull(recoveryInfoProvider);
        ArgumentNullException.ThrowIfNull(recoveryOptions);
        ArgumentNullException.ThrowIfNull(recoveryOptions.Value);
        ArgumentNullException.ThrowIfNull(sagaAccessAuthorizer);
        ArgumentNullException.ThrowIfNull(sagaAccessContextProvider);
        ArgumentNullException.ThrowIfNull(stepInfoProvider);
        ArgumentNullException.ThrowIfNull(timeProvider);
        AggregateGrainFactory = aggregateGrainFactory;
        BrookGrainFactory = brookGrainFactory;
        SnapshotGrainFactory = snapshotGrainFactory;
        RecoveryInfoProvider = recoveryInfoProvider;
        RecoveryOptions = recoveryOptions.Value;
        SagaAccessAuthorizer = sagaAccessAuthorizer;
        SagaAccessContextProvider = sagaAccessContextProvider;
        StepInfoProvider = stepInfoProvider;
        TimeProvider = timeProvider;
        Planner = new(stepInfoProvider, recoveryInfoProvider, Options.Create(RecoveryOptions));
    }

    private IAggregateGrainFactory AggregateGrainFactory { get; }

    private IBrookGrainFactory BrookGrainFactory { get; }

    private SagaRecoveryPlanner<TSaga> Planner { get; }

    private ISagaRecoveryInfoProvider<TSaga> RecoveryInfoProvider { get; }

    private SagaRecoveryOptions RecoveryOptions { get; }

    private ISagaAccessAuthorizer SagaAccessAuthorizer { get; }

    private ISagaAccessContextProvider SagaAccessContextProvider { get; }

    private ISnapshotGrainFactory SnapshotGrainFactory { get; }

    private ISagaStepInfoProvider<TSaga> StepInfoProvider { get; }

    private TimeProvider TimeProvider { get; }

    private static string? BuildAcceptedMessage(
        SagaRecoveryPlan plan
    ) =>
        plan.Disposition switch
        {
            SagaRecoveryPlanDisposition.ExecuteStep => "Manual resume accepted.",
            SagaRecoveryPlanDisposition.CompleteSaga => "Manual resume completed the saga.",
            SagaRecoveryPlanDisposition.CompensateSaga => "Manual resume completed saga compensation.",
            var _ => null,
        };

    private static SagaResumeResponse CreateResumeResponse(
        Guid sagaId,
        SagaRecoveryPlan plan,
        DateTimeOffset processedAt,
        SagaResumeRequestDisposition disposition,
        string? message
    ) =>
        new()
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

    private static bool IsWorkflowMismatchBlockedReason(
        string? blockedReason
    )
    {
        if (string.IsNullOrWhiteSpace(blockedReason))
        {
            return false;
        }

        return string.Equals(
                   blockedReason,
                   "Workflow hash mismatch prevents automatic resume.",
                   StringComparison.Ordinal) ||
               string.Equals(
                   blockedReason,
                   "Recovery checkpoint is missing a pending step index.",
                   StringComparison.Ordinal) ||
               (blockedReason.StartsWith("Pending step index '", StringComparison.Ordinal) &&
                blockedReason.EndsWith(
                    "' is not registered in the current saga definition.",
                    StringComparison.Ordinal)) ||
               (blockedReason.StartsWith("Step '", StringComparison.Ordinal) &&
                blockedReason.EndsWith(
                    "' no longer supports compensation recovery.",
                    StringComparison.Ordinal));
    }

    private static SagaResumeDisposition DetermineStatusDisposition(
        TSaga state,
        SagaRecoveryCheckpoint? checkpoint,
        bool workflowHashMatches,
        SagaRecoveryMode recoveryMode,
        SagaRecoveryPlan reminderPlan,
        bool automaticRecoveryEnabled
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
            return IsWorkflowMismatchBlockedReason(checkpoint.BlockedReason)
                ? SagaResumeDisposition.WorkflowMismatch
                : SagaResumeDisposition.ManualInterventionRequired;
        }

        if (checkpoint?.PendingDirection is not null)
        {
            if (!checkpoint.PendingStepIndex.HasValue)
            {
                return SagaResumeDisposition.WorkflowMismatch;
            }

            return recoveryMode is SagaRecoveryMode.ManualOnly || !automaticRecoveryEnabled
                ? SagaResumeDisposition.ManualInterventionRequired
                : reminderPlan.Disposition switch
                {
                    SagaRecoveryPlanDisposition.Blocked => SagaResumeDisposition.ManualInterventionRequired,
                    SagaRecoveryPlanDisposition.WorkflowMismatch => SagaResumeDisposition.WorkflowMismatch,
                    SagaRecoveryPlanDisposition.ExecuteStep or SagaRecoveryPlanDisposition.CompleteSaga
                        or SagaRecoveryPlanDisposition.CompensateSaga => SagaResumeDisposition.AutomaticPending,
                    var _ => SagaResumeDisposition.Idle,
                };
        }

        return SagaResumeDisposition.Idle;
    }

    private static SagaResumeRequestDisposition MapRequestDisposition(
        SagaRecoveryPlanDisposition disposition
    ) =>
        disposition switch
        {
            SagaRecoveryPlanDisposition.ExecuteStep or SagaRecoveryPlanDisposition.CompleteSaga
                or SagaRecoveryPlanDisposition.CompensateSaga => SagaResumeRequestDisposition.Accepted,
            SagaRecoveryPlanDisposition.Blocked => SagaResumeRequestDisposition.Blocked,
            SagaRecoveryPlanDisposition.Terminal => SagaResumeRequestDisposition.Terminal,
            SagaRecoveryPlanDisposition.WorkflowMismatch => SagaResumeRequestDisposition.WorkflowMismatch,
            var _ => SagaResumeRequestDisposition.NoAction,
        };

    /// <inheritdoc />
    public async Task<SagaRuntimeStatus?> GetRuntimeStatusAsync(
        string entityId,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);
        (TSaga? state, SagaRecoveryCheckpoint? checkpoint) =
            await LoadStateAndCheckpointAsync(entityId, cancellationToken);
        if (state is null)
        {
            return null;
        }

        if (!Authorize(entityId, SagaAccessAction.ReadRuntimeStatus, checkpoint).IsAuthorized)
        {
            return null;
        }

        SagaRecoveryMode recoveryMode = checkpoint?.RecoveryMode ?? RecoveryInfoProvider.Recovery.Mode;
        bool automaticRecoveryEnabled = RecoveryOptions.Enabled && !RecoveryOptions.ForceManualOnly;
        bool workflowHashMatches = state.Phase is SagaPhase.NotStarted || HasMatchingWorkflowHash(state, checkpoint);
        SagaRecoveryPlan reminderPlan = Plan(state, checkpoint, SagaResumeSource.Reminder);
        SagaResumeDisposition resumeDisposition = DetermineStatusDisposition(
            state,
            checkpoint,
            workflowHashMatches,
            recoveryMode,
            reminderPlan,
            automaticRecoveryEnabled);
        return new()
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
            ReminderArmed = resumeDisposition is SagaResumeDisposition.AutomaticPending,
            ResumeDisposition = resumeDisposition,
            SagaId = state.SagaId,
            WorkflowHashMatches = workflowHashMatches,
        };
    }

    /// <inheritdoc />
    public async Task<TSaga?> GetStateAsync(
        string entityId,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);
        (TSaga? state, SagaRecoveryCheckpoint? checkpoint) =
            await LoadStateAndCheckpointAsync(entityId, cancellationToken);
        if (state is null)
        {
            return null;
        }

        return Authorize(entityId, SagaAccessAction.ReadState, checkpoint).IsAuthorized ? state : null;
    }

    /// <inheritdoc />
    public async Task<SagaResumeResponse?> ResumeAsync(
        string entityId,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);
        (TSaga? state, SagaRecoveryCheckpoint? checkpoint) =
            await LoadStateAndCheckpointAsync(entityId, cancellationToken);
        if (state is null)
        {
            return null;
        }

        DateTimeOffset processedAt = TimeProvider.GetUtcNow();
        SagaAccessAuthorizationResult authorization = Authorize(entityId, SagaAccessAction.Resume, checkpoint);
        if (!authorization.IsAuthorized)
        {
            return new()
            {
                Disposition = SagaResumeRequestDisposition.Unauthorized,
                Message = authorization.FailureReason ?? "The current caller is not authorized to resume this saga.",
                ProcessedAt = processedAt,
                SagaId = state.SagaId,
                Source = SagaResumeSource.Manual,
            };
        }

        SagaRecoveryPlan plan = Plan(state, checkpoint, SagaResumeSource.Manual);
        if (plan.Disposition is SagaRecoveryPlanDisposition.ExecuteStep or SagaRecoveryPlanDisposition.Blocked
            or SagaRecoveryPlanDisposition.CompleteSaga or SagaRecoveryPlanDisposition.CompensateSaga)
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
                return CreateResumeResponse(
                    state.SagaId,
                    plan,
                    processedAt,
                    SagaResumeRequestDisposition.NoAction,
                    commandResult.ErrorMessage);
            }
        }

        return CreateResumeResponse(
            state.SagaId,
            plan,
            processedAt,
            MapRequestDisposition(plan.Disposition),
            plan.Reason);
    }

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

    private bool HasMatchingWorkflowHash(
        TSaga state,
        SagaRecoveryCheckpoint? checkpoint
    )
    {
        string expectedHash = SagaStepHash.Compute(RecoveryInfoProvider.Recovery, StepInfoProvider.Steps);
        string? actualHash = string.IsNullOrWhiteSpace(checkpoint?.StepHash) ? state.StepHash : checkpoint.StepHash;
        return !string.IsNullOrWhiteSpace(actualHash) &&
               string.Equals(actualHash, expectedHash, StringComparison.Ordinal);
    }

    private async Task<(TSaga? State, SagaRecoveryCheckpoint? Checkpoint)> LoadStateAndCheckpointAsync(
        string entityId,
        CancellationToken cancellationToken
    )
    {
        IGenericAggregateGrain<TSaga> grain = AggregateGrainFactory.GetGenericAggregate<TSaga>(entityId);
        TSaga? state = await grain.GetStateAsync(cancellationToken);
        SagaRecoveryCheckpoint? checkpoint = null;
        if (state is not null)
        {
            BrookKey brookKey = BrookKey.ForType<TSaga>(entityId);
            BrookPosition currentPosition =
                await BrookGrainFactory.GetBrookCursorGrain(brookKey).GetLatestPositionAsync();
            if (!currentPosition.NotSet)
            {
                SnapshotStreamKey streamKey = new(
                    brookKey.BrookName,
                    SnapshotStorageNameHelper.GetStorageName<SagaRecoveryCheckpoint>(),
                    brookKey.EntityId,
                    SagaRecoveryCheckpointMetadata.CheckpointReducerHash);
                SnapshotKey snapshotKey = new(streamKey, currentPosition.Value);
                checkpoint = await SnapshotGrainFactory.GetSnapshotCacheGrain<SagaRecoveryCheckpoint>(snapshotKey)
                    .GetStateAsync(cancellationToken);
            }
        }

        return (state, checkpoint);
    }

    private SagaRecoveryPlan Plan(
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
}