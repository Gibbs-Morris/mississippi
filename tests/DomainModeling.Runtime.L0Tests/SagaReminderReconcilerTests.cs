using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Abstractions.Attributes;
using Mississippi.Brooks.Abstractions.Cursor;
using Mississippi.Brooks.Abstractions.Factory;
using Mississippi.DomainModeling.Abstractions;
using Mississippi.Tributary.Abstractions;

using Moq;

using Orleans;
using Orleans.Runtime;


namespace Mississippi.DomainModeling.Runtime.L0Tests;

/// <summary>
///     Tests for <see cref="SagaReminderReconciler{TSaga}" />.
/// </summary>
public sealed class SagaReminderReconcilerTests
{
    private static readonly ReminderSagaState RunningState = new() { Phase = SagaPhase.Running };

    /// <summary>
    ///     Saga state used to exercise reminder reconciliation against a brook-backed saga type.
    /// </summary>
    [BrookName("TEST", "SAGAS", "RECONCILER")]
    internal sealed record ReminderSagaState : ISagaState
    {
        /// <summary>
        ///     Gets the correlation identifier.
        /// </summary>
        public string? CorrelationId { get; init; }

        /// <summary>
        ///     Gets the last completed step index.
        /// </summary>
        public int LastCompletedStepIndex { get; init; } = -1;

        /// <summary>
        ///     Gets the saga phase.
        /// </summary>
        public SagaPhase Phase { get; init; }

        /// <summary>
        ///     Gets the saga identifier.
        /// </summary>
        public Guid SagaId { get; init; }

        /// <summary>
        ///     Gets the timestamp when the saga started.
        /// </summary>
        public DateTimeOffset? StartedAt { get; init; }

        /// <summary>
        ///     Gets the stored workflow hash.
        /// </summary>
        public string? StepHash { get; init; }
    }

    private static SagaRecoveryCheckpoint CreateCheckpoint(
        DateTimeOffset? nextEligibleResumeAt = null,
        string? blockedReason = null,
        SagaRecoveryMode recoveryMode = SagaRecoveryMode.Automatic,
        int automaticAttemptCount = 0,
        SagaExecutionDirection? pendingDirection = SagaExecutionDirection.Forward,
        int? pendingStepIndex = 0,
        bool reminderArmed = false
    ) => new()
    {
        AutomaticAttemptCount = automaticAttemptCount,
        BlockedReason = blockedReason,
        NextEligibleResumeAt = nextEligibleResumeAt,
        PendingDirection = pendingDirection,
        PendingStepIndex = pendingStepIndex,
        ReminderArmed = reminderArmed,
        RecoveryMode = recoveryMode,
        SagaId = Guid.NewGuid(),
        StepHash = "hash",
    };

    private static Mock<IGrainBase> CreateGrain(
        string entityId = "saga-123"
    )
    {
        Mock<IGrainContext> grainContextMock = new();
        grainContextMock.Setup(context => context.GrainId).Returns(GrainId.Create("test", entityId));
        Mock<IGrainBase> grainMock = new();
        grainMock.Setup(grain => grain.GrainContext).Returns(grainContextMock.Object);
        return grainMock;
    }

    private static SagaReminderReconciler<ReminderSagaState> CreateReconciler(
        Mock<IGrainReminderManager> reminderManagerMock,
        Mock<IBrookGrainFactory>? brookGrainFactoryMock = null,
        Mock<ISnapshotGrainFactory>? snapshotGrainFactoryMock = null,
        SagaRecoveryOptions? options = null,
        FakeTimeProvider? timeProvider = null
    )
    {
        brookGrainFactoryMock ??= new();
        snapshotGrainFactoryMock ??= new();
        Mock<IRootReducer<SagaRecoveryCheckpoint>> checkpointReducerMock = new();
        checkpointReducerMock.Setup(reducer => reducer.GetReducerHash()).Returns("checkpoint-hash");
        SagaRecoveryCheckpointAccessor<ReminderSagaState> checkpointAccessor = new(
            brookGrainFactoryMock.Object,
            snapshotGrainFactoryMock.Object,
            checkpointReducerMock.Object);
        return new SagaReminderReconciler<ReminderSagaState>(
            checkpointAccessor,
            reminderManagerMock.Object,
            Options.Create(options ?? new SagaRecoveryOptions()),
            timeProvider ?? new FakeTimeProvider(new DateTimeOffset(2025, 2, 15, 12, 0, 0, TimeSpan.Zero)),
            Mock.Of<ILogger<SagaReminderReconciler<ReminderSagaState>>>());
    }

    private static void SetupCheckpoint(
        Mock<IBrookGrainFactory> brookGrainFactoryMock,
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock,
        SagaRecoveryCheckpoint? checkpoint
    )
    {
        Mock<IBrookCursorGrain> cursorGrainMock = new();
        brookGrainFactoryMock.Setup(factory => factory.GetBrookCursorGrain(It.IsAny<BrookKey>()))
            .Returns(cursorGrainMock.Object);
        if (checkpoint is null)
        {
            cursorGrainMock.Setup(cursor => cursor.GetLatestPositionAsync()).ReturnsAsync(new BrookPosition());
            return;
        }

        cursorGrainMock.Setup(cursor => cursor.GetLatestPositionAsync()).ReturnsAsync(new BrookPosition(3));
        Mock<ISnapshotCacheGrain<SagaRecoveryCheckpoint>> snapshotCacheGrainMock = new();
        snapshotCacheGrainMock.Setup(snapshot => snapshot.GetStateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(checkpoint);
        snapshotGrainFactoryMock.Setup(factory => factory.GetSnapshotCacheGrain<SagaRecoveryCheckpoint>(It.IsAny<SnapshotKey>()))
            .Returns(snapshotCacheGrainMock.Object);
    }

    /// <summary>
    ///     Verifies the constructor rejects missing dependencies.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenDependenciesMissing()
    {
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        Mock<IRootReducer<SagaRecoveryCheckpoint>> checkpointReducerMock = new();
        checkpointReducerMock.Setup(reducer => reducer.GetReducerHash()).Returns("checkpoint-hash");
        SagaRecoveryCheckpointAccessor<ReminderSagaState> checkpointAccessor = new(
            brookGrainFactoryMock.Object,
            snapshotGrainFactoryMock.Object,
            checkpointReducerMock.Object);
        Mock<IGrainReminderManager> reminderManagerMock = new();
        IOptions<SagaRecoveryOptions> nullValueOptions = Mock.Of<IOptions<SagaRecoveryOptions>>(options => options.Value == null!);
        FakeTimeProvider timeProvider = new();
        ILogger<SagaReminderReconciler<ReminderSagaState>> logger = Mock.Of<ILogger<SagaReminderReconciler<ReminderSagaState>>>();

        Assert.Throws<ArgumentNullException>(() => new SagaReminderReconciler<ReminderSagaState>(
            null!,
            reminderManagerMock.Object,
            Options.Create(new SagaRecoveryOptions()),
            timeProvider,
            logger));
        Assert.Throws<ArgumentNullException>(() => new SagaReminderReconciler<ReminderSagaState>(
            checkpointAccessor,
            null!,
            Options.Create(new SagaRecoveryOptions()),
            timeProvider,
            logger));
        Assert.Throws<ArgumentNullException>(() => new SagaReminderReconciler<ReminderSagaState>(
            checkpointAccessor,
            reminderManagerMock.Object,
            null!,
            timeProvider,
            logger));
        Assert.Throws<ArgumentNullException>(() => new SagaReminderReconciler<ReminderSagaState>(
            checkpointAccessor,
            reminderManagerMock.Object,
            nullValueOptions,
            timeProvider,
            logger));
        Assert.Throws<ArgumentNullException>(() => new SagaReminderReconciler<ReminderSagaState>(
            checkpointAccessor,
            reminderManagerMock.Object,
            Options.Create(new SagaRecoveryOptions()),
            null!,
            logger));
        Assert.Throws<ArgumentNullException>(() => new SagaReminderReconciler<ReminderSagaState>(
            checkpointAccessor,
            reminderManagerMock.Object,
            Options.Create(new SagaRecoveryOptions()),
            timeProvider,
            null!));
    }

    /// <summary>
    ///     Verifies eligible sagas register a recovery reminder using the configured fallback due time.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ReconcileAsyncRegistersReminderWhenSagaIsEligible()
    {
        Mock<IGrainReminderManager> reminderManagerMock = new();
        reminderManagerMock.Setup(manager => manager.GetReminderAsync(It.IsAny<IGrainBase>(), SagaReminderNames.Recovery))
            .ReturnsAsync((IGrainReminder?)null);
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        SetupCheckpoint(brookGrainFactoryMock, snapshotGrainFactoryMock, CreateCheckpoint());
        SagaReminderReconciler<ReminderSagaState> reconciler = CreateReconciler(
            reminderManagerMock,
            brookGrainFactoryMock,
            snapshotGrainFactoryMock);
        Mock<IGrainBase> grainMock = CreateGrain();

        await reconciler.ReconcileAsync(
            grainMock.Object,
            "saga-123",
            _ => Task.FromResult<ReminderSagaState?>(RunningState));

        reminderManagerMock.Verify(
            manager => manager.RegisterOrUpdateReminderAsync(
                grainMock.Object,
                SagaReminderNames.Recovery,
                TimeSpan.FromMinutes(1),
                TimeSpan.FromMinutes(5)),
            Times.Once);
        reminderManagerMock.Verify(
            manager => manager.UnregisterReminderAsync(It.IsAny<IGrainBase>(), It.IsAny<IGrainReminder>()),
            Times.Never);
    }

    /// <summary>
    ///     Verifies future eligibility timestamps delay reminder registration accordingly.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ReconcileAsyncUsesNextEligibleResumeAtForFutureDueTime()
    {
        Mock<IGrainReminderManager> reminderManagerMock = new();
        Mock<IGrainReminder> existingReminderMock = new();
        reminderManagerMock.Setup(manager => manager.GetReminderAsync(It.IsAny<IGrainBase>(), SagaReminderNames.Recovery))
            .ReturnsAsync(existingReminderMock.Object);
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        DateTimeOffset now = new(2025, 2, 15, 12, 0, 0, TimeSpan.Zero);
        SetupCheckpoint(
            brookGrainFactoryMock,
            snapshotGrainFactoryMock,
            CreateCheckpoint(nextEligibleResumeAt: now.AddMinutes(2)));
        SagaReminderReconciler<ReminderSagaState> reconciler = CreateReconciler(
            reminderManagerMock,
            brookGrainFactoryMock,
            snapshotGrainFactoryMock,
            timeProvider: new FakeTimeProvider(now));
        Mock<IGrainBase> grainMock = CreateGrain();

        await reconciler.ReconcileAsync(
            grainMock.Object,
            "saga-123",
            _ => Task.FromResult<ReminderSagaState?>(RunningState));

        reminderManagerMock.Verify(
            manager => manager.RegisterOrUpdateReminderAsync(
                grainMock.Object,
                SagaReminderNames.Recovery,
                TimeSpan.FromMinutes(2),
                TimeSpan.FromMinutes(5)),
            Times.Once);
    }

    /// <summary>
    ///     Verifies overdue checkpoints schedule an immediate reminder tick.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ReconcileAsyncSchedulesImmediateReminderWhenCheckpointIsOverdue()
    {
        Mock<IGrainReminderManager> reminderManagerMock = new();
        reminderManagerMock.Setup(manager => manager.GetReminderAsync(It.IsAny<IGrainBase>(), SagaReminderNames.Recovery))
            .ReturnsAsync((IGrainReminder?)null);
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        DateTimeOffset now = new(2025, 2, 15, 12, 0, 0, TimeSpan.Zero);
        SetupCheckpoint(
            brookGrainFactoryMock,
            snapshotGrainFactoryMock,
            CreateCheckpoint(nextEligibleResumeAt: now.AddMinutes(-1)));
        SagaReminderReconciler<ReminderSagaState> reconciler = CreateReconciler(
            reminderManagerMock,
            brookGrainFactoryMock,
            snapshotGrainFactoryMock,
            timeProvider: new FakeTimeProvider(now));
        Mock<IGrainBase> grainMock = CreateGrain();

        await reconciler.ReconcileAsync(
            grainMock.Object,
            "saga-123",
            _ => Task.FromResult<ReminderSagaState?>(RunningState));

        reminderManagerMock.Verify(
            manager => manager.RegisterOrUpdateReminderAsync(
                grainMock.Object,
                SagaReminderNames.Recovery,
                TimeSpan.Zero,
                TimeSpan.FromMinutes(5)),
            Times.Once);
    }

    /// <summary>
    ///     Verifies non-positive fallback due times are clamped to zero.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ReconcileAsyncClampsNonPositiveFallbackDueTimeToZero()
    {
        Mock<IGrainReminderManager> reminderManagerMock = new();
        reminderManagerMock.Setup(manager => manager.GetReminderAsync(It.IsAny<IGrainBase>(), SagaReminderNames.Recovery))
            .ReturnsAsync((IGrainReminder?)null);
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        SetupCheckpoint(brookGrainFactoryMock, snapshotGrainFactoryMock, CreateCheckpoint());
        SagaReminderReconciler<ReminderSagaState> reconciler = CreateReconciler(
            reminderManagerMock,
            brookGrainFactoryMock,
            snapshotGrainFactoryMock,
            new SagaRecoveryOptions { InitialReminderDueTime = TimeSpan.Zero });
        Mock<IGrainBase> grainMock = CreateGrain();

        await reconciler.ReconcileAsync(
            grainMock.Object,
            "saga-123",
            _ => Task.FromResult<ReminderSagaState?>(RunningState));

        reminderManagerMock.Verify(
            manager => manager.RegisterOrUpdateReminderAsync(
                grainMock.Object,
                SagaReminderNames.Recovery,
                TimeSpan.Zero,
                TimeSpan.FromMinutes(5)),
            Times.Once);
    }

    /// <summary>
    ///     Verifies terminal saga states clear any stale reminder.
    /// </summary>
    /// <param name="phase">The terminal phase to evaluate.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Theory]
    [InlineData(SagaPhase.Completed)]
    [InlineData(SagaPhase.Compensated)]
    [InlineData(SagaPhase.Failed)]
    public async Task ReconcileAsyncClearsReminderWhenSagaIsTerminal(
        SagaPhase phase
    )
    {
        Mock<IGrainReminderManager> reminderManagerMock = new();
        Mock<IGrainReminder> existingReminderMock = new();
        reminderManagerMock.Setup(manager => manager.GetReminderAsync(It.IsAny<IGrainBase>(), SagaReminderNames.Recovery))
            .ReturnsAsync(existingReminderMock.Object);
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        SetupCheckpoint(brookGrainFactoryMock, snapshotGrainFactoryMock, CreateCheckpoint(reminderArmed: true));
        SagaReminderReconciler<ReminderSagaState> reconciler = CreateReconciler(
            reminderManagerMock,
            brookGrainFactoryMock,
            snapshotGrainFactoryMock);
        Mock<IGrainBase> grainMock = CreateGrain();

        await reconciler.ReconcileAsync(
            grainMock.Object,
            "saga-123",
            _ => Task.FromResult<ReminderSagaState?>(new ReminderSagaState { Phase = phase }));

        reminderManagerMock.Verify(
            manager => manager.UnregisterReminderAsync(grainMock.Object, existingReminderMock.Object),
            Times.Once);
    }

    /// <summary>
    ///     Verifies blocked checkpoints clear any stale reminder.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ReconcileAsyncClearsReminderWhenCheckpointIsBlocked()
    {
        Mock<IGrainReminderManager> reminderManagerMock = new();
        Mock<IGrainReminder> existingReminderMock = new();
        reminderManagerMock.Setup(manager => manager.GetReminderAsync(It.IsAny<IGrainBase>(), SagaReminderNames.Recovery))
            .ReturnsAsync(existingReminderMock.Object);
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        SetupCheckpoint(
            brookGrainFactoryMock,
            snapshotGrainFactoryMock,
            CreateCheckpoint(blockedReason: "manual intervention required", reminderArmed: true));
        SagaReminderReconciler<ReminderSagaState> reconciler = CreateReconciler(
            reminderManagerMock,
            brookGrainFactoryMock,
            snapshotGrainFactoryMock);
        Mock<IGrainBase> grainMock = CreateGrain();

        await reconciler.ReconcileAsync(
            grainMock.Object,
            "saga-123",
            _ => Task.FromResult<ReminderSagaState?>(RunningState));

        reminderManagerMock.Verify(
            manager => manager.UnregisterReminderAsync(grainMock.Object, existingReminderMock.Object),
            Times.Once);
        reminderManagerMock.Verify(
            manager => manager.RegisterOrUpdateReminderAsync(
                It.IsAny<IGrainBase>(),
                It.IsAny<string>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<TimeSpan>()),
            Times.Never);
    }

    /// <summary>
    ///     Verifies manual-only recovery mode clears automatic reminders.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ReconcileAsyncClearsReminderWhenRecoveryModeIsManualOnly()
    {
        Mock<IGrainReminderManager> reminderManagerMock = new();
        Mock<IGrainReminder> existingReminderMock = new();
        reminderManagerMock.Setup(manager => manager.GetReminderAsync(It.IsAny<IGrainBase>(), SagaReminderNames.Recovery))
            .ReturnsAsync(existingReminderMock.Object);
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        SetupCheckpoint(
            brookGrainFactoryMock,
            snapshotGrainFactoryMock,
            CreateCheckpoint(recoveryMode: SagaRecoveryMode.ManualOnly, reminderArmed: true));
        SagaReminderReconciler<ReminderSagaState> reconciler = CreateReconciler(
            reminderManagerMock,
            brookGrainFactoryMock,
            snapshotGrainFactoryMock);
        Mock<IGrainBase> grainMock = CreateGrain();

        await reconciler.ReconcileAsync(
            grainMock.Object,
            "saga-123",
            _ => Task.FromResult<ReminderSagaState?>(RunningState));

        reminderManagerMock.Verify(
            manager => manager.UnregisterReminderAsync(grainMock.Object, existingReminderMock.Object),
            Times.Once);
    }

    /// <summary>
    ///     Verifies max-attempt exhaustion clears automatic reminders.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ReconcileAsyncClearsReminderWhenAutomaticAttemptsAreExhausted()
    {
        Mock<IGrainReminderManager> reminderManagerMock = new();
        Mock<IGrainReminder> existingReminderMock = new();
        reminderManagerMock.Setup(manager => manager.GetReminderAsync(It.IsAny<IGrainBase>(), SagaReminderNames.Recovery))
            .ReturnsAsync(existingReminderMock.Object);
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        SetupCheckpoint(
            brookGrainFactoryMock,
            snapshotGrainFactoryMock,
            CreateCheckpoint(automaticAttemptCount: 2, reminderArmed: true));
        SagaReminderReconciler<ReminderSagaState> reconciler = CreateReconciler(
            reminderManagerMock,
            brookGrainFactoryMock,
            snapshotGrainFactoryMock,
            new SagaRecoveryOptions { MaxAutomaticAttempts = 2 });
        Mock<IGrainBase> grainMock = CreateGrain();

        await reconciler.ReconcileAsync(
            grainMock.Object,
            "saga-123",
            _ => Task.FromResult<ReminderSagaState?>(RunningState));

        reminderManagerMock.Verify(
            manager => manager.UnregisterReminderAsync(grainMock.Object, existingReminderMock.Object),
            Times.Once);
    }

    /// <summary>
    ///     Verifies reminders are cleared when no pending direction exists.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ReconcileAsyncClearsReminderWhenPendingDirectionMissing()
    {
        Mock<IGrainReminderManager> reminderManagerMock = new();
        Mock<IGrainReminder> existingReminderMock = new();
        reminderManagerMock.Setup(manager => manager.GetReminderAsync(It.IsAny<IGrainBase>(), SagaReminderNames.Recovery))
            .ReturnsAsync(existingReminderMock.Object);
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        SetupCheckpoint(
            brookGrainFactoryMock,
            snapshotGrainFactoryMock,
            CreateCheckpoint(pendingDirection: null, reminderArmed: true));
        SagaReminderReconciler<ReminderSagaState> reconciler = CreateReconciler(
            reminderManagerMock,
            brookGrainFactoryMock,
            snapshotGrainFactoryMock);
        Mock<IGrainBase> grainMock = CreateGrain();

        await reconciler.ReconcileAsync(
            grainMock.Object,
            "saga-123",
            _ => Task.FromResult<ReminderSagaState?>(RunningState));

        reminderManagerMock.Verify(
            manager => manager.UnregisterReminderAsync(grainMock.Object, existingReminderMock.Object),
            Times.Once);
    }

    /// <summary>
    ///     Verifies reminders are cleared when no pending step index exists.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ReconcileAsyncClearsReminderWhenPendingStepIndexMissing()
    {
        Mock<IGrainReminderManager> reminderManagerMock = new();
        Mock<IGrainReminder> existingReminderMock = new();
        reminderManagerMock.Setup(manager => manager.GetReminderAsync(It.IsAny<IGrainBase>(), SagaReminderNames.Recovery))
            .ReturnsAsync(existingReminderMock.Object);
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        SetupCheckpoint(
            brookGrainFactoryMock,
            snapshotGrainFactoryMock,
            CreateCheckpoint(pendingStepIndex: null, reminderArmed: true));
        SagaReminderReconciler<ReminderSagaState> reconciler = CreateReconciler(
            reminderManagerMock,
            brookGrainFactoryMock,
            snapshotGrainFactoryMock);
        Mock<IGrainBase> grainMock = CreateGrain();

        await reconciler.ReconcileAsync(
            grainMock.Object,
            "saga-123",
            _ => Task.FromResult<ReminderSagaState?>(RunningState));

        reminderManagerMock.Verify(
            manager => manager.UnregisterReminderAsync(grainMock.Object, existingReminderMock.Object),
            Times.Once);
    }

    /// <summary>
    ///     Verifies disabled runtime recovery clears any existing reminder.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ReconcileAsyncClearsReminderWhenRecoveryIsDisabled()
    {
        Mock<IGrainReminderManager> reminderManagerMock = new();
        Mock<IGrainReminder> existingReminderMock = new();
        reminderManagerMock.Setup(manager => manager.GetReminderAsync(It.IsAny<IGrainBase>(), SagaReminderNames.Recovery))
            .ReturnsAsync(existingReminderMock.Object);
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        SetupCheckpoint(brookGrainFactoryMock, snapshotGrainFactoryMock, CreateCheckpoint(reminderArmed: true));
        SagaReminderReconciler<ReminderSagaState> reconciler = CreateReconciler(
            reminderManagerMock,
            brookGrainFactoryMock,
            snapshotGrainFactoryMock,
            new SagaRecoveryOptions { Enabled = false });
        Mock<IGrainBase> grainMock = CreateGrain();

        await reconciler.ReconcileAsync(
            grainMock.Object,
            "saga-123",
            _ => Task.FromResult<ReminderSagaState?>(RunningState));

        reminderManagerMock.Verify(
            manager => manager.UnregisterReminderAsync(grainMock.Object, existingReminderMock.Object),
            Times.Once);
    }

    /// <summary>
    ///     Verifies disabled recovery still checks for and removes stale reminders even when the checkpoint says no reminder is armed.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ReconcileAsyncClearsStaleReminderWhenReminderArmedFlagIsFalse()
    {
        Mock<IGrainReminderManager> reminderManagerMock = new();
        Mock<IGrainReminder> existingReminderMock = new();
        reminderManagerMock.Setup(manager => manager.GetReminderAsync(It.IsAny<IGrainBase>(), SagaReminderNames.Recovery))
            .ReturnsAsync(existingReminderMock.Object);
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        SetupCheckpoint(brookGrainFactoryMock, snapshotGrainFactoryMock, CreateCheckpoint());
        SagaReminderReconciler<ReminderSagaState> reconciler = CreateReconciler(
            reminderManagerMock,
            brookGrainFactoryMock,
            snapshotGrainFactoryMock,
            new SagaRecoveryOptions { Enabled = false });
        Mock<IGrainBase> grainMock = CreateGrain();

        await reconciler.ReconcileAsync(
            grainMock.Object,
            "saga-123",
            _ => Task.FromResult<ReminderSagaState?>(RunningState));

        reminderManagerMock.Verify(
            manager => manager.GetReminderAsync(It.IsAny<IGrainBase>(), It.IsAny<string>()),
            Times.Once);
        reminderManagerMock.Verify(
            manager => manager.UnregisterReminderAsync(grainMock.Object, existingReminderMock.Object),
            Times.Once);
    }
}