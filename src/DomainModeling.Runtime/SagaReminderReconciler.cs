using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.Brooks.Abstractions;
using Mississippi.DomainModeling.Abstractions;

using Orleans;
using Orleans.Runtime;


namespace Mississippi.DomainModeling.Runtime;

/// <summary>
///     Reconciles Orleans reminder registration with the authoritative saga recovery checkpoint.
/// </summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
internal sealed class SagaReminderReconciler<TSaga> : IAggregateReminderReconciler<TSaga>
    where TSaga : class, ISagaState
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SagaReminderReconciler{TSaga}" /> class.
    /// </summary>
    /// <param name="checkpointAccessor">Loads the latest saga recovery checkpoint.</param>
    /// <param name="grainReminderManager">Manages Orleans reminder registrations.</param>
    /// <param name="recoveryOptions">Runtime reminder recovery options.</param>
    /// <param name="timeProvider">Time provider used to compute reminder due times deterministically.</param>
    /// <param name="logger">Logger instance.</param>
    public SagaReminderReconciler(
        SagaRecoveryCheckpointAccessor<TSaga> checkpointAccessor,
        IGrainReminderManager grainReminderManager,
        IOptions<SagaRecoveryOptions> recoveryOptions,
        TimeProvider timeProvider,
        ILogger<SagaReminderReconciler<TSaga>> logger
    )
    {
        CheckpointAccessor = checkpointAccessor ?? throw new ArgumentNullException(nameof(checkpointAccessor));
        GrainReminderManager = grainReminderManager ?? throw new ArgumentNullException(nameof(grainReminderManager));
        RecoveryOptions = recoveryOptions?.Value ?? throw new ArgumentNullException(nameof(recoveryOptions));
        TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private SagaRecoveryCheckpointAccessor<TSaga> CheckpointAccessor { get; }

    private IGrainReminderManager GrainReminderManager { get; }

    private ILogger<SagaReminderReconciler<TSaga>> Logger { get; }

    private SagaRecoveryOptions RecoveryOptions { get; }

    private TimeProvider TimeProvider { get; }

    /// <inheritdoc />
    public async Task ReconcileAsync(
        IGrainBase grain,
        string entityId,
        Func<CancellationToken, Task<TSaga?>> loadStateAsync,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(grain);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);
        ArgumentNullException.ThrowIfNull(loadStateAsync);
        string aggregateKey = BrookKey.ForType<TSaga>(entityId);
        TSaga? state = await loadStateAsync(cancellationToken);
        SagaRecoveryCheckpoint? checkpoint = await CheckpointAccessor.GetAsync(entityId, cancellationToken);
        bool shouldArmReminder = ShouldArmReminder(state, checkpoint);
        IGrainReminder? existingReminder =
            await GrainReminderManager.GetReminderAsync(grain, SagaReminderNames.Recovery);
        if (!shouldArmReminder)
        {
            Logger.ReminderReconciled(aggregateKey, SagaReminderNames.Recovery, "clear");
            if (existingReminder is not null)
            {
                await GrainReminderManager.UnregisterReminderAsync(grain, existingReminder);
                Logger.ReminderRemoved(SagaReminderNames.Recovery, aggregateKey);
            }

            return;
        }

        TimeSpan dueTime = CalculateDueTime(checkpoint!);
        Logger.ReminderReconciled(
            aggregateKey,
            SagaReminderNames.Recovery,
            existingReminder is null ? "register" : "update");
        await GrainReminderManager.RegisterOrUpdateReminderAsync(
            grain,
            SagaReminderNames.Recovery,
            dueTime,
            RecoveryOptions.ReminderPeriod);
        Logger.ReminderScheduled(SagaReminderNames.Recovery, aggregateKey, dueTime, RecoveryOptions.ReminderPeriod);
    }

    private TimeSpan CalculateDueTime(
        SagaRecoveryCheckpoint checkpoint
    )
    {
        DateTimeOffset now = TimeProvider.GetUtcNow();
        if (checkpoint.NextEligibleResumeAt is null)
        {
            return RecoveryOptions.InitialReminderDueTime > TimeSpan.Zero
                ? RecoveryOptions.InitialReminderDueTime
                : TimeSpan.Zero;
        }

        TimeSpan dueTime = checkpoint.NextEligibleResumeAt.Value - now;
        return dueTime > TimeSpan.Zero ? dueTime : TimeSpan.Zero;
    }

    private bool ShouldArmReminder(
        TSaga? state,
        SagaRecoveryCheckpoint? checkpoint
    )
    {
        if (!RecoveryOptions.Enabled || RecoveryOptions.ForceManualOnly || state is null || checkpoint is null)
        {
            return false;
        }

        if (state.Phase is SagaPhase.Completed or SagaPhase.Compensated or SagaPhase.Failed)
        {
            return false;
        }

        if (checkpoint.RecoveryMode is not SagaRecoveryMode.Automatic)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(checkpoint.BlockedReason))
        {
            return false;
        }

        if (checkpoint.PendingDirection is null || !checkpoint.PendingStepIndex.HasValue)
        {
            return false;
        }

        return checkpoint.AutomaticAttemptCount < RecoveryOptions.MaxAutomaticAttempts;
    }
}