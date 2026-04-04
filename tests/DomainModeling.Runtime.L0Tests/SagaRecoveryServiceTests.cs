using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Abstractions.Attributes;
using Mississippi.Brooks.Abstractions.Cursor;
using Mississippi.Brooks.Abstractions.Factory;
using Mississippi.DomainModeling.Abstractions;
using Mississippi.Tributary.Abstractions;

using Moq;


namespace Mississippi.DomainModeling.Runtime.L0Tests;

/// <summary>
///     Tests for <see cref="SagaRecoveryService{TSaga}" />.
/// </summary>
public sealed class SagaRecoveryServiceTests
{
    private static readonly IReadOnlyList<SagaStepInfo> Steps =
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
        SagaRecoveryMode mode = SagaRecoveryMode.Automatic
    ) =>
        SagaStepHash.Compute(new(mode, null), Steps);

    private static SagaRecoveryCheckpoint CreateCheckpoint(
        SagaExecutionDirection direction,
        int stepIndex,
        string? blockedReason = null,
        SagaRecoveryMode recoveryMode = SagaRecoveryMode.Automatic,
        string? stepHash = null,
        string? accessContextFingerprint = null
    ) =>
        new()
        {
            AccessContextFingerprint = accessContextFingerprint,
            AutomaticAttemptCount = 2,
            BlockedReason = blockedReason,
            LastResumeAttemptedAt = new(2026, 4, 4, 18, 0, 0, TimeSpan.Zero),
            LastResumeSource = SagaResumeSource.Reminder,
            NextEligibleResumeAt = new(2026, 4, 4, 18, 5, 0, TimeSpan.Zero),
            PendingDirection = direction,
            PendingStepIndex = stepIndex,
            PendingStepName = stepIndex == 0 ? "Debit" : "Credit",
            RecoveryMode = recoveryMode,
            ReminderArmed = true,
            SagaId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            StepHash = stepHash ?? ComputeHash(recoveryMode),
        };

    private static SagaRecoveryService<ServiceSagaState> CreateService(
        IAggregateGrainFactory aggregateGrainFactory,
        FakeTimeProvider timeProvider,
        SagaRecoveryMode recoveryMode = SagaRecoveryMode.Automatic,
        SagaRecoveryOptions? recoveryOptions = null,
        ISagaAccessContextProvider? sagaAccessContextProvider = null,
        ISagaAccessAuthorizer? sagaAccessAuthorizer = null,
        Mock<IBrookGrainFactory>? brookGrainFactoryMock = null,
        Mock<ISnapshotGrainFactory>? snapshotGrainFactoryMock = null
    )
    {
        bool configureDefaultCursor = brookGrainFactoryMock is null;
        brookGrainFactoryMock ??= new();
        snapshotGrainFactoryMock ??= new();
        if (configureDefaultCursor)
        {
            Mock<IBrookCursorGrain> defaultCursorGrainMock = new();
            defaultCursorGrainMock.Setup(g => g.GetLatestPositionAsync()).ReturnsAsync(new BrookPosition());
            brookGrainFactoryMock.Setup(f => f.GetBrookCursorGrain(It.IsAny<BrookKey>()))
                .Returns(defaultCursorGrainMock.Object);
        }

        return new(
            aggregateGrainFactory,
            brookGrainFactoryMock.Object,
            snapshotGrainFactoryMock.Object,
            new SagaRecoveryInfoProvider<ServiceSagaState>(new(recoveryMode, null)),
            Options.Create(recoveryOptions ?? new SagaRecoveryOptions()),
            sagaAccessAuthorizer ?? new DefaultSagaAccessAuthorizer(),
            sagaAccessContextProvider ?? new DefaultSagaAccessContextProvider(),
            new SagaStepInfoProvider<ServiceSagaState>(Steps),
            timeProvider);
    }

    private static void SetupCheckpointLoad(
        Mock<IBrookGrainFactory> brookGrainFactoryMock,
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock,
        string entityId,
        SagaRecoveryCheckpoint checkpoint,
        long position = 7
    )
    {
        BrookKey brookKey = BrookKey.ForType<ServiceSagaState>(entityId);
        Mock<IBrookCursorGrain> cursorGrainMock = new();
        cursorGrainMock.Setup(g => g.GetLatestPositionAsync()).ReturnsAsync(new BrookPosition(position));
        brookGrainFactoryMock.Setup(f => f.GetBrookCursorGrain(brookKey)).Returns(cursorGrainMock.Object);
        SnapshotKey snapshotKey = new(
            new(
                brookKey.BrookName,
                SnapshotStorageNameHelper.GetStorageName<SagaRecoveryCheckpoint>(),
                brookKey.EntityId,
                SagaRecoveryCheckpointMetadata.CheckpointReducerHash),
            position);
        Mock<ISnapshotCacheGrain<SagaRecoveryCheckpoint>> snapshotCacheGrainMock = new();
        snapshotCacheGrainMock.Setup(g => g.GetStateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(checkpoint);
        snapshotGrainFactoryMock.Setup(f => f.GetSnapshotCacheGrain<SagaRecoveryCheckpoint>(snapshotKey))
            .Returns(snapshotCacheGrainMock.Object);
    }

    private sealed class FakeAggregateGrain : IGenericAggregateGrain<ServiceSagaState>
    {
        public OperationResult ExecuteResult { get; set; } = OperationResult.Ok();

        public object? LastCommand { get; private set; }

        public ServiceSagaState? State { get; set; }

        public Task<OperationResult> ExecuteAsync(
            object command,
            CancellationToken cancellationToken = default
        )
        {
            LastCommand = command;
            return Task.FromResult(ExecuteResult);
        }

        public Task<OperationResult> ExecuteAsync(
            object command,
            BrookPosition? expectedVersion,
            CancellationToken cancellationToken = default
        ) =>
            ExecuteAsync(command, cancellationToken);

        public Task<ServiceSagaState?> GetStateAsync(
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(State);
    }

    private sealed class FakeAggregateGrainFactory : IAggregateGrainFactory
    {
        public required FakeAggregateGrain Grain { get; init; }

        public IGenericAggregateGrain<TAggregate> GetGenericAggregate<TAggregate>(
            string entityId
        )
            where TAggregate : class =>
            (IGenericAggregateGrain<TAggregate>)(object)Grain;

        public IGenericAggregateGrain<TAggregate> GetGenericAggregate<TAggregate>(
            AggregateKey aggregateKey
        )
            where TAggregate : class =>
            (IGenericAggregateGrain<TAggregate>)(object)Grain;
    }

    [BrookName("TEST", "SAGAS", "SERVICE")]
    private sealed record ServiceSagaState : ISagaState
    {
        public string? CorrelationId { get; init; }

        public int LastCompletedStepIndex { get; } = -1;

        public SagaPhase Phase { get; init; }

        public Guid SagaId { get; init; }

        public DateTimeOffset? StartedAt { get; init; }

        public string? StepHash { get; init; }
    }

    /// <summary>
    ///     Verifies runtime status reports automatic-pending state when resumable work exists.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task GetRuntimeStatusAsyncReturnsAutomaticPendingWhenCheckpointHasPendingWork()
    {
        FakeAggregateGrainFactory aggregateGrainFactory = new()
        {
            Grain = new()
            {
                State = new()
                {
                    Phase = SagaPhase.Running,
                    SagaId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    StepHash = ComputeHash(),
                },
            },
        };
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        SetupCheckpointLoad(
            brookGrainFactoryMock,
            snapshotGrainFactoryMock,
            "saga-123",
            CreateCheckpoint(SagaExecutionDirection.Forward, 0));
        SagaRecoveryService<ServiceSagaState> service = CreateService(
            aggregateGrainFactory,
            new(new(2026, 4, 4, 19, 0, 0, TimeSpan.Zero)),
            brookGrainFactoryMock: brookGrainFactoryMock,
            snapshotGrainFactoryMock: snapshotGrainFactoryMock);
        SagaRuntimeStatus? status = await service.GetRuntimeStatusAsync("saga-123");
        Assert.NotNull(status);
        Assert.Equal(SagaResumeDisposition.AutomaticPending, status.ResumeDisposition);
        Assert.True(status.WorkflowHashMatches);
        Assert.Equal(SagaExecutionDirection.Forward, status.PendingDirection);
        Assert.Equal(0, status.PendingStepIndex);
        Assert.Equal("Debit", status.PendingStepName);
        Assert.Equal(SagaRecoveryMode.Automatic, status.RecoveryMode);
    }

    /// <summary>
    ///     Verifies runtime status falls back to idle when a not-started saga has no recovery checkpoint yet.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task GetRuntimeStatusAsyncReturnsIdleWhenSagaNotStarted()
    {
        FakeAggregateGrainFactory aggregateGrainFactory = new()
        {
            Grain = new()
            {
                State = new()
                {
                    Phase = SagaPhase.NotStarted,
                    SagaId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                },
            },
        };
        Mock<IBrookCursorGrain> cursorGrainMock = new();
        cursorGrainMock.Setup(g => g.GetLatestPositionAsync()).ReturnsAsync(new BrookPosition());
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        brookGrainFactoryMock.Setup(f => f.GetBrookCursorGrain(It.IsAny<BrookKey>())).Returns(cursorGrainMock.Object);
        SagaRecoveryService<ServiceSagaState> service = CreateService(
            aggregateGrainFactory,
            new(new(2026, 4, 4, 19, 15, 0, TimeSpan.Zero)),
            brookGrainFactoryMock: brookGrainFactoryMock);
        SagaRuntimeStatus? status = await service.GetRuntimeStatusAsync("saga-123");
        Assert.NotNull(status);
        Assert.Equal(SagaResumeDisposition.Idle, status.ResumeDisposition);
        Assert.True(status.WorkflowHashMatches);
    }

    /// <summary>
    ///     Verifies runtime status reports manual intervention when automatic recovery is globally forced into manual-only
    ///     mode.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task GetRuntimeStatusAsyncReturnsManualInterventionRequiredWhenAutomaticRecoveryForcedManual()
    {
        FakeAggregateGrainFactory aggregateGrainFactory = new()
        {
            Grain = new()
            {
                State = new()
                {
                    Phase = SagaPhase.Running,
                    SagaId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    StepHash = ComputeHash(),
                },
            },
        };
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        SetupCheckpointLoad(
            brookGrainFactoryMock,
            snapshotGrainFactoryMock,
            "saga-123",
            CreateCheckpoint(SagaExecutionDirection.Forward, 0));
        SagaRecoveryService<ServiceSagaState> service = CreateService(
            aggregateGrainFactory,
            new(new(2026, 4, 4, 19, 0, 0, TimeSpan.Zero)),
            recoveryOptions: new()
            {
                ForceManualOnly = true,
            },
            brookGrainFactoryMock: brookGrainFactoryMock,
            snapshotGrainFactoryMock: snapshotGrainFactoryMock);
        SagaRuntimeStatus? status = await service.GetRuntimeStatusAsync("saga-123");
        Assert.NotNull(status);
        Assert.Equal(SagaResumeDisposition.ManualInterventionRequired, status.ResumeDisposition);
        Assert.False(status.ReminderArmed);
        Assert.Equal(SagaRecoveryMode.Automatic, status.RecoveryMode);
    }

    /// <summary>
    ///     Verifies runtime status reports manual intervention when checkpoint metadata is explicitly blocked.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task GetRuntimeStatusAsyncReturnsManualInterventionRequiredWhenCheckpointBlocked()
    {
        FakeAggregateGrainFactory aggregateGrainFactory = new()
        {
            Grain = new()
            {
                State = new()
                {
                    Phase = SagaPhase.Compensating,
                    SagaId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    StepHash = ComputeHash(),
                },
            },
        };
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        SetupCheckpointLoad(
            brookGrainFactoryMock,
            snapshotGrainFactoryMock,
            "saga-123",
            CreateCheckpoint(SagaExecutionDirection.Compensation, 0, "Manual approval required."));
        SagaRecoveryService<ServiceSagaState> service = CreateService(
            aggregateGrainFactory,
            new(new(2026, 4, 4, 19, 0, 0, TimeSpan.Zero)),
            brookGrainFactoryMock: brookGrainFactoryMock,
            snapshotGrainFactoryMock: snapshotGrainFactoryMock);
        SagaRuntimeStatus? status = await service.GetRuntimeStatusAsync("saga-123");
        Assert.NotNull(status);
        Assert.Equal(SagaResumeDisposition.ManualInterventionRequired, status.ResumeDisposition);
        Assert.Equal("Manual approval required.", status.BlockedReason);
    }

    /// <summary>
    ///     Verifies runtime status reports manual intervention when the saga is configured manual-only.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task GetRuntimeStatusAsyncReturnsManualInterventionRequiredWhenSagaIsManualOnly()
    {
        FakeAggregateGrainFactory aggregateGrainFactory = new()
        {
            Grain = new()
            {
                State = new()
                {
                    Phase = SagaPhase.Running,
                    SagaId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    StepHash = ComputeHash(SagaRecoveryMode.ManualOnly),
                },
            },
        };
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        SetupCheckpointLoad(
            brookGrainFactoryMock,
            snapshotGrainFactoryMock,
            "saga-123",
            CreateCheckpoint(SagaExecutionDirection.Forward, 0, recoveryMode: SagaRecoveryMode.ManualOnly));
        SagaRecoveryService<ServiceSagaState> service = CreateService(
            aggregateGrainFactory,
            new(new(2026, 4, 4, 19, 0, 0, TimeSpan.Zero)),
            SagaRecoveryMode.ManualOnly,
            brookGrainFactoryMock: brookGrainFactoryMock,
            snapshotGrainFactoryMock: snapshotGrainFactoryMock);
        SagaRuntimeStatus? status = await service.GetRuntimeStatusAsync("saga-123");
        Assert.NotNull(status);
        Assert.Equal(SagaResumeDisposition.ManualInterventionRequired, status.ResumeDisposition);
        Assert.Null(status.BlockedReason);
        Assert.Equal(SagaRecoveryMode.ManualOnly, status.RecoveryMode);
    }

    /// <summary>
    ///     Verifies runtime status returns null when the saga state is not found.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task GetRuntimeStatusAsyncReturnsNullWhenSagaStateMissing()
    {
        FakeAggregateGrainFactory aggregateGrainFactory = new()
        {
            Grain = new(),
        };
        SagaRecoveryService<ServiceSagaState> service = CreateService(
            aggregateGrainFactory,
            new(new(2026, 4, 4, 19, 0, 0, TimeSpan.Zero)));
        SagaRuntimeStatus? status = await service.GetRuntimeStatusAsync("saga-123");
        Assert.Null(status);
    }

    /// <summary>
    ///     Verifies runtime status returns terminal when the saga is already complete.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task GetRuntimeStatusAsyncReturnsTerminalWhenSagaAlreadyTerminal()
    {
        FakeAggregateGrainFactory aggregateGrainFactory = new()
        {
            Grain = new()
            {
                State = new()
                {
                    Phase = SagaPhase.Completed,
                    SagaId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    StepHash = ComputeHash(),
                },
            },
        };
        SagaRecoveryService<ServiceSagaState> service = CreateService(
            aggregateGrainFactory,
            new(new(2026, 4, 4, 19, 0, 0, TimeSpan.Zero)));
        SagaRuntimeStatus? status = await service.GetRuntimeStatusAsync("saga-123");
        Assert.NotNull(status);
        Assert.Equal(SagaResumeDisposition.Terminal, status.ResumeDisposition);
    }

    /// <summary>
    ///     Verifies runtime status reports workflow mismatch when both checkpoint and state hashes are absent.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task GetRuntimeStatusAsyncReturnsWorkflowMismatchWhenHashesAreMissing()
    {
        FakeAggregateGrainFactory aggregateGrainFactory = new()
        {
            Grain = new()
            {
                State = new()
                {
                    Phase = SagaPhase.Running,
                    SagaId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    StepHash = null,
                },
            },
        };
        Mock<IBrookCursorGrain> cursorGrainMock = new();
        cursorGrainMock.Setup(g => g.GetLatestPositionAsync()).ReturnsAsync(new BrookPosition());
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        brookGrainFactoryMock.Setup(f => f.GetBrookCursorGrain(It.IsAny<BrookKey>())).Returns(cursorGrainMock.Object);
        SagaRecoveryService<ServiceSagaState> service = CreateService(
            aggregateGrainFactory,
            new(new(2026, 4, 4, 19, 20, 0, TimeSpan.Zero)),
            brookGrainFactoryMock: brookGrainFactoryMock);
        SagaRuntimeStatus? status = await service.GetRuntimeStatusAsync("saga-123");
        Assert.NotNull(status);
        Assert.Equal(SagaResumeDisposition.WorkflowMismatch, status.ResumeDisposition);
        Assert.False(status.WorkflowHashMatches);
    }

    /// <summary>
    ///     Verifies runtime status reports workflow mismatch when persisted recovery metadata drifts.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task GetRuntimeStatusAsyncReturnsWorkflowMismatchWhenHashesDiffer()
    {
        FakeAggregateGrainFactory aggregateGrainFactory = new()
        {
            Grain = new()
            {
                State = new()
                {
                    Phase = SagaPhase.Running,
                    SagaId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    StepHash = "legacy-hash",
                },
            },
        };
        Mock<IBrookCursorGrain> cursorGrainMock = new();
        cursorGrainMock.Setup(g => g.GetLatestPositionAsync()).ReturnsAsync(new BrookPosition());
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        brookGrainFactoryMock.Setup(f => f.GetBrookCursorGrain(It.IsAny<BrookKey>())).Returns(cursorGrainMock.Object);
        SagaRecoveryService<ServiceSagaState> service = CreateService(
            aggregateGrainFactory,
            new(new(2026, 4, 4, 19, 0, 0, TimeSpan.Zero)),
            brookGrainFactoryMock: brookGrainFactoryMock);
        SagaRuntimeStatus? status = await service.GetRuntimeStatusAsync("saga-123");
        Assert.NotNull(status);
        Assert.Equal(SagaResumeDisposition.WorkflowMismatch, status.ResumeDisposition);
        Assert.False(status.WorkflowHashMatches);
    }

    /// <summary>
    ///     Verifies raw saga state reads fail closed when the caller fingerprint does not match the stored fingerprint.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task GetStateAsyncReturnsNullWhenCallerNotAuthorized()
    {
        FakeAggregateGrainFactory aggregateGrainFactory = new()
        {
            Grain = new()
            {
                State = new()
                {
                    Phase = SagaPhase.Running,
                    SagaId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    StepHash = ComputeHash(),
                },
            },
        };
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        SetupCheckpointLoad(
            brookGrainFactoryMock,
            snapshotGrainFactoryMock,
            "saga-123",
            CreateCheckpoint(SagaExecutionDirection.Forward, 0, accessContextFingerprint: "tenant:user-a"));
        Mock<ISagaAccessContextProvider> sagaAccessContextProviderMock = new();
        sagaAccessContextProviderMock.Setup(p => p.GetFingerprint()).Returns("tenant:user-b");
        SagaRecoveryService<ServiceSagaState> service = CreateService(
            aggregateGrainFactory,
            new(new(2026, 4, 4, 18, 45, 0, TimeSpan.Zero)),
            sagaAccessContextProvider: sagaAccessContextProviderMock.Object,
            brookGrainFactoryMock: brookGrainFactoryMock,
            snapshotGrainFactoryMock: snapshotGrainFactoryMock);
        ServiceSagaState? state = await service.GetStateAsync("saga-123");
        Assert.Null(state);
    }

    /// <summary>
    ///     Verifies the recovery service can still expose raw saga state for generated read surfaces.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task GetStateAsyncReturnsStateFromGrain()
    {
        ServiceSagaState expectedState = new()
        {
            Phase = SagaPhase.Running,
            SagaId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            StepHash = ComputeHash(),
        };
        FakeAggregateGrainFactory aggregateGrainFactory = new()
        {
            Grain = new()
            {
                State = expectedState,
            },
        };
        SagaRecoveryService<ServiceSagaState> service = CreateService(
            aggregateGrainFactory,
            new(new(2026, 4, 4, 18, 30, 0, TimeSpan.Zero)));
        ServiceSagaState? state = await service.GetStateAsync("saga-123");
        Assert.Same(expectedState, state);
    }

    /// <summary>
    ///     Verifies blocked helper mapping preserves the blocked reason for response payloads.
    /// </summary>
    [Fact]
    public void PrivateHelpersMapBlockedPlansToBlockedResponses()
    {
        MethodInfo mapRequestDispositionMethod = typeof(SagaRecoveryService<ServiceSagaState>).GetMethod(
            "MapRequestDisposition",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        SagaResumeRequestDisposition disposition = (SagaResumeRequestDisposition)mapRequestDispositionMethod.Invoke(
            null,
            [SagaRecoveryPlanDisposition.Blocked])!;
        MethodInfo createResumeResponseMethod = typeof(SagaRecoveryService<ServiceSagaState>).GetMethod(
            "CreateResumeResponse",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        SagaRecoveryPlan blockedPlan = new()
        {
            Direction = SagaExecutionDirection.Forward,
            Disposition = SagaRecoveryPlanDisposition.Blocked,
            Reason = "Operator approval required.",
            Step = Steps[0],
        };
        SagaResumeResponse response = (SagaResumeResponse)createResumeResponseMethod.Invoke(
            null,
            [
                Guid.Parse("33333333-3333-3333-3333-333333333333"),
                blockedPlan,
                new DateTimeOffset(2026, 4, 4, 21, 45, 0, TimeSpan.Zero),
                disposition,
                blockedPlan.Reason,
            ])!;
        Assert.Equal(SagaResumeRequestDisposition.Blocked, disposition);
        Assert.Equal("Operator approval required.", response.BlockedReason);
        Assert.Equal(SagaResumeRequestDisposition.Blocked, response.Disposition);
    }

    /// <summary>
    ///     Verifies manual resume can complete saga compensation when rollback work is exhausted.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task ResumeAsyncReturnsAcceptedWhenPlannerCompletesCompensation()
    {
        FakeAggregateGrain grain = new()
        {
            State = new()
            {
                Phase = SagaPhase.Compensating,
                SagaId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                StepHash = ComputeHash(),
            },
        };
        FakeAggregateGrainFactory aggregateGrainFactory = new()
        {
            Grain = grain,
        };
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        SetupCheckpointLoad(
            brookGrainFactoryMock,
            snapshotGrainFactoryMock,
            "saga-123",
            CreateCheckpoint(SagaExecutionDirection.Compensation, -1));
        FakeTimeProvider timeProvider = new(new(2026, 4, 4, 20, 20, 0, TimeSpan.Zero));
        SagaRecoveryService<ServiceSagaState> service = CreateService(
            aggregateGrainFactory,
            timeProvider,
            brookGrainFactoryMock: brookGrainFactoryMock,
            snapshotGrainFactoryMock: snapshotGrainFactoryMock);
        SagaResumeResponse? response = await service.ResumeAsync("saga-123");
        Assert.NotNull(response);
        Assert.Equal(SagaResumeRequestDisposition.Accepted, response.Disposition);
        Assert.Equal("Manual resume completed saga compensation.", response.Message);
    }

    /// <summary>
    ///     Verifies manual resume can complete a saga when forward work is already exhausted.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task ResumeAsyncReturnsAcceptedWhenPlannerCompletesSaga()
    {
        FakeAggregateGrain grain = new()
        {
            State = new()
            {
                Phase = SagaPhase.Running,
                SagaId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                StepHash = ComputeHash(),
            },
        };
        FakeAggregateGrainFactory aggregateGrainFactory = new()
        {
            Grain = grain,
        };
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        SetupCheckpointLoad(
            brookGrainFactoryMock,
            snapshotGrainFactoryMock,
            "saga-123",
            CreateCheckpoint(SagaExecutionDirection.Forward, 2));
        FakeTimeProvider timeProvider = new(new(2026, 4, 4, 20, 10, 0, TimeSpan.Zero));
        SagaRecoveryService<ServiceSagaState> service = CreateService(
            aggregateGrainFactory,
            timeProvider,
            brookGrainFactoryMock: brookGrainFactoryMock,
            snapshotGrainFactoryMock: snapshotGrainFactoryMock);
        SagaResumeResponse? response = await service.ResumeAsync("saga-123");
        Assert.NotNull(response);
        Assert.Equal(SagaResumeRequestDisposition.Accepted, response.Disposition);
        Assert.Equal("Manual resume completed the saga.", response.Message);
    }

    /// <summary>
    ///     Verifies manual resume accepts actionable execution plans.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task ResumeAsyncReturnsAcceptedWhenPlannerExecutesStep()
    {
        FakeAggregateGrain grain = new()
        {
            State = new()
            {
                Phase = SagaPhase.Running,
                SagaId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                StepHash = ComputeHash(),
            },
        };
        FakeAggregateGrainFactory aggregateGrainFactory = new()
        {
            Grain = grain,
        };
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        SetupCheckpointLoad(
            brookGrainFactoryMock,
            snapshotGrainFactoryMock,
            "saga-123",
            CreateCheckpoint(SagaExecutionDirection.Forward, 0));
        FakeTimeProvider timeProvider = new(new(2026, 4, 4, 20, 0, 0, TimeSpan.Zero));
        Mock<ISagaAccessContextProvider> sagaAccessContextProviderMock = new();
        sagaAccessContextProviderMock.Setup(p => p.GetFingerprint()).Returns("tenant:user-a");
        SagaRecoveryService<ServiceSagaState> service = CreateService(
            aggregateGrainFactory,
            timeProvider,
            sagaAccessContextProvider: sagaAccessContextProviderMock.Object,
            brookGrainFactoryMock: brookGrainFactoryMock,
            snapshotGrainFactoryMock: snapshotGrainFactoryMock);
        SagaResumeResponse? response = await service.ResumeAsync("saga-123");
        Assert.NotNull(response);
        Assert.Equal(SagaResumeRequestDisposition.Accepted, response.Disposition);
        Assert.Equal("Manual resume accepted.", response.Message);
        Assert.Equal(0, response.PendingStepIndex);
        Assert.Equal("Debit", response.PendingStepName);
        Assert.Equal(timeProvider.GetUtcNow(), response.ProcessedAt);
        Assert.Equal(SagaResumeSource.Manual, response.Source);
        ResumeSagaCommand command = Assert.IsType<ResumeSagaCommand>(grain.LastCommand);
        Assert.Equal("tenant:user-a", command.AccessContextFingerprint);
    }

    /// <summary>
    ///     Verifies manual resume returns a no-action response when checkpoint metadata is unavailable.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task ResumeAsyncReturnsNoActionWhenCheckpointMissing()
    {
        FakeAggregateGrainFactory aggregateGrainFactory = new()
        {
            Grain = new()
            {
                State = new()
                {
                    Phase = SagaPhase.Running,
                    SagaId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    StepHash = ComputeHash(),
                },
            },
        };
        Mock<IBrookCursorGrain> cursorGrainMock = new();
        cursorGrainMock.Setup(g => g.GetLatestPositionAsync()).ReturnsAsync(new BrookPosition());
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        brookGrainFactoryMock.Setup(f => f.GetBrookCursorGrain(It.IsAny<BrookKey>())).Returns(cursorGrainMock.Object);
        FakeTimeProvider timeProvider = new(new(2026, 4, 4, 21, 0, 0, TimeSpan.Zero));
        SagaRecoveryService<ServiceSagaState> service = CreateService(
            aggregateGrainFactory,
            timeProvider,
            brookGrainFactoryMock: brookGrainFactoryMock);
        SagaResumeResponse? response = await service.ResumeAsync("saga-123");
        Assert.NotNull(response);
        Assert.Equal(SagaResumeRequestDisposition.NoAction, response.Disposition);
        Assert.Equal("Recovery checkpoint not found.", response.Message);
    }

    /// <summary>
    ///     Verifies failed command execution is surfaced as a no-action manual resume response.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task ResumeAsyncReturnsNoActionWhenCommandExecutionFails()
    {
        FakeAggregateGrain grain = new()
        {
            ExecuteResult = OperationResult.Fail("FAIL", "resume failed"),
            State = new()
            {
                Phase = SagaPhase.Running,
                SagaId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                StepHash = ComputeHash(),
            },
        };
        FakeAggregateGrainFactory aggregateGrainFactory = new()
        {
            Grain = grain,
        };
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        SetupCheckpointLoad(
            brookGrainFactoryMock,
            snapshotGrainFactoryMock,
            "saga-123",
            CreateCheckpoint(SagaExecutionDirection.Forward, 0));
        SagaRecoveryService<ServiceSagaState> service = CreateService(
            aggregateGrainFactory,
            new(new(2026, 4, 4, 21, 30, 0, TimeSpan.Zero)),
            brookGrainFactoryMock: brookGrainFactoryMock,
            snapshotGrainFactoryMock: snapshotGrainFactoryMock);
        SagaResumeResponse? response = await service.ResumeAsync("saga-123");
        Assert.NotNull(response);
        Assert.Equal(SagaResumeRequestDisposition.NoAction, response.Disposition);
        Assert.Equal("resume failed", response.Message);
    }

    /// <summary>
    ///     Verifies manual resume returns null when the saga state is not found.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task ResumeAsyncReturnsNullWhenSagaStateMissing()
    {
        FakeAggregateGrainFactory aggregateGrainFactory = new()
        {
            Grain = new(),
        };
        SagaRecoveryService<ServiceSagaState> service = CreateService(
            aggregateGrainFactory,
            new(new(2026, 4, 4, 20, 0, 0, TimeSpan.Zero)));
        SagaResumeResponse? response = await service.ResumeAsync("saga-123");
        Assert.Null(response);
    }

    /// <summary>
    ///     Verifies manual resume reports terminal sagas without issuing commands.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task ResumeAsyncReturnsTerminalWhenSagaAlreadyTerminal()
    {
        FakeAggregateGrain grain = new()
        {
            State = new()
            {
                Phase = SagaPhase.Completed,
                SagaId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                StepHash = ComputeHash(),
            },
        };
        FakeAggregateGrainFactory aggregateGrainFactory = new()
        {
            Grain = grain,
        };
        FakeTimeProvider timeProvider = new(new(2026, 4, 4, 20, 30, 0, TimeSpan.Zero));
        SagaRecoveryService<ServiceSagaState> service = CreateService(aggregateGrainFactory, timeProvider);
        SagaResumeResponse? response = await service.ResumeAsync("saga-123");
        Assert.NotNull(response);
        Assert.Equal(SagaResumeRequestDisposition.Terminal, response.Disposition);
        Assert.Null(response.Message);
        Assert.Null(grain.LastCommand);
    }

    /// <summary>
    ///     Verifies manual resume returns a typed unauthorized response when the caller fingerprint does not match.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task ResumeAsyncReturnsUnauthorizedWhenCallerNotAuthorized()
    {
        FakeAggregateGrain grain = new()
        {
            State = new()
            {
                Phase = SagaPhase.Running,
                SagaId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                StepHash = ComputeHash(),
            },
        };
        FakeAggregateGrainFactory aggregateGrainFactory = new()
        {
            Grain = grain,
        };
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        SetupCheckpointLoad(
            brookGrainFactoryMock,
            snapshotGrainFactoryMock,
            "saga-123",
            CreateCheckpoint(SagaExecutionDirection.Forward, 0, accessContextFingerprint: "tenant:user-a"));
        Mock<ISagaAccessContextProvider> sagaAccessContextProviderMock = new();
        sagaAccessContextProviderMock.Setup(p => p.GetFingerprint()).Returns("tenant:user-b");
        FakeTimeProvider timeProvider = new(new(2026, 4, 4, 20, 5, 0, TimeSpan.Zero));
        SagaRecoveryService<ServiceSagaState> service = CreateService(
            aggregateGrainFactory,
            timeProvider,
            sagaAccessContextProvider: sagaAccessContextProviderMock.Object,
            brookGrainFactoryMock: brookGrainFactoryMock,
            snapshotGrainFactoryMock: snapshotGrainFactoryMock);
        SagaResumeResponse? response = await service.ResumeAsync("saga-123");
        Assert.NotNull(response);
        Assert.Equal(SagaResumeRequestDisposition.Unauthorized, response.Disposition);
        Assert.Equal("The current caller is not authorized for this saga.", response.Message);
        Assert.Equal(SagaResumeSource.Manual, response.Source);
        Assert.Null(grain.LastCommand);
    }

    /// <summary>
    ///     Verifies manual resume surfaces workflow mismatches without dispatching a resume command.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task ResumeAsyncReturnsWorkflowMismatchWhenHashesDiffer()
    {
        FakeAggregateGrain grain = new()
        {
            State = new()
            {
                Phase = SagaPhase.Running,
                SagaId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                StepHash = "legacy-hash",
            },
        };
        FakeAggregateGrainFactory aggregateGrainFactory = new()
        {
            Grain = grain,
        };
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        SetupCheckpointLoad(
            brookGrainFactoryMock,
            snapshotGrainFactoryMock,
            "saga-123",
            CreateCheckpoint(SagaExecutionDirection.Forward, 0, stepHash: "legacy-hash"));
        SagaRecoveryService<ServiceSagaState> service = CreateService(
            aggregateGrainFactory,
            new(new(2026, 4, 4, 20, 0, 0, TimeSpan.Zero)),
            brookGrainFactoryMock: brookGrainFactoryMock,
            snapshotGrainFactoryMock: snapshotGrainFactoryMock);
        SagaResumeResponse? response = await service.ResumeAsync("saga-123");
        Assert.NotNull(response);
        Assert.Equal(SagaResumeRequestDisposition.WorkflowMismatch, response.Disposition);
        Assert.Null(grain.LastCommand);
    }
}