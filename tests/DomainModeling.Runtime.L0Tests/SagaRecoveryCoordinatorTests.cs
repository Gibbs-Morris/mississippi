using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Abstractions.Attributes;
using Mississippi.Brooks.Abstractions.Cursor;
using Mississippi.Brooks.Abstractions.Factory;
using Mississippi.DomainModeling.Abstractions;
using Mississippi.Tributary.Abstractions;

using Moq;


namespace Mississippi.DomainModeling.Runtime.L0Tests;

/// <summary>
///     Tests for <see cref="SagaRecoveryCoordinator{TSaga}" />.
/// </summary>
public sealed class SagaRecoveryCoordinatorTests
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
        new(
            1,
            "Credit",
            typeof(SagaNonCompensatableStep),
            false,
            SagaStepRecoveryPolicy.ManualOnly,
            null),
    ];

    /// <summary>
    ///     Saga state used to exercise the recovery coordinator against a brook-backed saga type.
    /// </summary>
    [BrookName("TEST", "SAGAS", "COORDINATOR")]
    internal sealed record CoordinatorSagaState : ISagaState
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

    private static string ComputeHash() => SagaStepHash.Compute(new(SagaRecoveryMode.Automatic, null), Steps);

    private static SagaRecoveryCoordinator<CoordinatorSagaState> CreateCoordinator(
        Mock<IAggregateGrainFactory> aggregateGrainFactoryMock,
        Mock<IBrookGrainFactory>? brookGrainFactoryMock = null,
        Mock<ISnapshotGrainFactory>? snapshotGrainFactoryMock = null
    )
    {
        brookGrainFactoryMock ??= new();
        snapshotGrainFactoryMock ??= new();
        Mock<IRootReducer<SagaRecoveryCheckpoint>> checkpointReducerMock = new();
        checkpointReducerMock.Setup(r => r.GetReducerHash()).Returns("checkpoint-hash");
        return new(
            aggregateGrainFactoryMock.Object,
            new SagaRecoveryCheckpointAccessor<CoordinatorSagaState>(
                brookGrainFactoryMock.Object,
                snapshotGrainFactoryMock.Object,
                checkpointReducerMock.Object),
            new SagaRecoveryPlanner<CoordinatorSagaState>(
                new SagaStepInfoProvider<CoordinatorSagaState>(Steps),
                new SagaRecoveryInfoProvider<CoordinatorSagaState>(new(SagaRecoveryMode.Automatic, null))));
    }

    /// <summary>
    ///     Verifies invalid entity identifiers are rejected.
    /// </summary>
    /// <param name="entityId">The invalid entity identifier.</param>
    /// <returns>A task that represents the asynchronous assertion.</returns>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task PlanAsyncThrowsWhenEntityIdInvalid(
        string? entityId
    )
    {
        Mock<IAggregateGrainFactory> aggregateGrainFactoryMock = new();
        SagaRecoveryCoordinator<CoordinatorSagaState> coordinator = CreateCoordinator(aggregateGrainFactoryMock);

        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            coordinator.PlanAsync(entityId!, SagaResumeSource.Reminder));
    }

    /// <summary>
    ///     Verifies missing saga state produces a no-action plan without querying checkpoints.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task PlanAsyncReturnsNoActionWhenSagaStateMissing()
    {
        Mock<IGenericAggregateGrain<CoordinatorSagaState>> grainMock = new();
        grainMock.Setup(g => g.GetStateAsync(It.IsAny<CancellationToken>())).ReturnsAsync((CoordinatorSagaState?)null);
        Mock<IAggregateGrainFactory> aggregateGrainFactoryMock = new();
        aggregateGrainFactoryMock.Setup(f => f.GetGenericAggregate<CoordinatorSagaState>("saga-123"))
            .Returns(grainMock.Object);
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        SagaRecoveryCoordinator<CoordinatorSagaState> coordinator = CreateCoordinator(
            aggregateGrainFactoryMock,
            brookGrainFactoryMock: brookGrainFactoryMock);

        SagaRecoveryPlan plan = await coordinator.PlanAsync("saga-123", SagaResumeSource.Reminder);

        Assert.Equal(SagaRecoveryPlanDisposition.NoAction, plan.Disposition);
        Assert.Equal("Saga state not found.", plan.Reason);
        brookGrainFactoryMock.Verify(f => f.GetBrookCursorGrain(It.IsAny<BrookKey>()), Times.Never);
    }

    /// <summary>
    ///     Verifies terminal saga state short-circuits to a terminal plan without loading checkpoints.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task PlanAsyncReturnsTerminalWhenSagaAlreadyTerminal()
    {
        Mock<IGenericAggregateGrain<CoordinatorSagaState>> grainMock = new();
        grainMock.Setup(g => g.GetStateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new CoordinatorSagaState
        {
            Phase = SagaPhase.Completed,
            StepHash = ComputeHash(),
        });
        Mock<IAggregateGrainFactory> aggregateGrainFactoryMock = new();
        aggregateGrainFactoryMock.Setup(f => f.GetGenericAggregate<CoordinatorSagaState>("saga-123"))
            .Returns(grainMock.Object);
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        SagaRecoveryCoordinator<CoordinatorSagaState> coordinator = CreateCoordinator(
            aggregateGrainFactoryMock,
            brookGrainFactoryMock: brookGrainFactoryMock);

        SagaRecoveryPlan plan = await coordinator.PlanAsync("saga-123", SagaResumeSource.Reminder);

        Assert.Equal(SagaRecoveryPlanDisposition.Terminal, plan.Disposition);
        brookGrainFactoryMock.Verify(f => f.GetBrookCursorGrain(It.IsAny<BrookKey>()), Times.Never);
    }

    /// <summary>
    ///     Verifies missing checkpoints conservatively produce a no-action plan.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task PlanAsyncReturnsNoActionWhenCheckpointMissing()
    {
        Mock<IGenericAggregateGrain<CoordinatorSagaState>> grainMock = new();
        grainMock.Setup(g => g.GetStateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new CoordinatorSagaState
        {
            Phase = SagaPhase.Running,
            StepHash = ComputeHash(),
        });
        Mock<IAggregateGrainFactory> aggregateGrainFactoryMock = new();
        aggregateGrainFactoryMock.Setup(f => f.GetGenericAggregate<CoordinatorSagaState>("saga-123"))
            .Returns(grainMock.Object);
        Mock<IBrookCursorGrain> cursorGrainMock = new();
        cursorGrainMock.Setup(g => g.GetLatestPositionAsync()).ReturnsAsync(new BrookPosition());
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        brookGrainFactoryMock.Setup(f => f.GetBrookCursorGrain(It.IsAny<BrookKey>())).Returns(cursorGrainMock.Object);
        SagaRecoveryCoordinator<CoordinatorSagaState> coordinator = CreateCoordinator(
            aggregateGrainFactoryMock,
            brookGrainFactoryMock: brookGrainFactoryMock);

        SagaRecoveryPlan plan = await coordinator.PlanAsync("saga-123", SagaResumeSource.Reminder);

        Assert.Equal(SagaRecoveryPlanDisposition.NoAction, plan.Disposition);
        Assert.Equal("Recovery checkpoint not found.", plan.Reason);
    }

    /// <summary>
    ///     Verifies reminder planning delegates to the recovery planner once state and checkpoint are loaded.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task PlanAsyncReturnsPlannerDecisionForReminder()
    {
        Mock<IGenericAggregateGrain<CoordinatorSagaState>> grainMock = new();
        grainMock.Setup(g => g.GetStateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new CoordinatorSagaState
        {
            Phase = SagaPhase.Running,
            StepHash = ComputeHash(),
        });
        Mock<IAggregateGrainFactory> aggregateGrainFactoryMock = new();
        aggregateGrainFactoryMock.Setup(f => f.GetGenericAggregate<CoordinatorSagaState>("saga-123"))
            .Returns(grainMock.Object);
        BrookKey brookKey = BrookKey.ForType<CoordinatorSagaState>("saga-123");
        Mock<IBrookCursorGrain> cursorGrainMock = new();
        cursorGrainMock.Setup(g => g.GetLatestPositionAsync()).ReturnsAsync(new BrookPosition(7));
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        brookGrainFactoryMock.Setup(f => f.GetBrookCursorGrain(brookKey)).Returns(cursorGrainMock.Object);
        SnapshotKey snapshotKey = new(
            new(
                brookKey.BrookName,
                SnapshotStorageNameHelper.GetStorageName<SagaRecoveryCheckpoint>(),
                brookKey.EntityId,
                "checkpoint-hash"),
            7);
        SagaRecoveryCheckpoint checkpoint = new()
        {
            PendingDirection = SagaExecutionDirection.Forward,
            PendingStepIndex = 1,
            RecoveryMode = SagaRecoveryMode.Automatic,
            SagaId = Guid.NewGuid(),
            StepHash = ComputeHash(),
        };
        Mock<ISnapshotCacheGrain<SagaRecoveryCheckpoint>> snapshotCacheGrainMock = new();
        snapshotCacheGrainMock.Setup(g => g.GetStateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(checkpoint);
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        snapshotGrainFactoryMock.Setup(f => f.GetSnapshotCacheGrain<SagaRecoveryCheckpoint>(snapshotKey))
            .Returns(snapshotCacheGrainMock.Object);
        SagaRecoveryCoordinator<CoordinatorSagaState> coordinator = CreateCoordinator(
            aggregateGrainFactoryMock,
            brookGrainFactoryMock,
            snapshotGrainFactoryMock);

        SagaRecoveryPlan plan = await coordinator.PlanAsync("saga-123", SagaResumeSource.Reminder);

        Assert.Equal(SagaRecoveryPlanDisposition.Blocked, plan.Disposition);
        Assert.Equal(SagaExecutionDirection.Forward, plan.Direction);
        Assert.Equal("Step 'Credit' requires manual forward recovery.", plan.Reason);
        Assert.NotNull(plan.Step);
        Assert.Equal(1, plan.Step.StepIndex);
    }

    /// <summary>
    ///     Verifies manual sources can still execute manual-only steps through the delegated planner.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task PlanAsyncReturnsPlannerDecisionForManualResume()
    {
        Mock<IGenericAggregateGrain<CoordinatorSagaState>> grainMock = new();
        grainMock.Setup(g => g.GetStateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new CoordinatorSagaState
        {
            Phase = SagaPhase.Running,
            StepHash = ComputeHash(),
        });
        Mock<IAggregateGrainFactory> aggregateGrainFactoryMock = new();
        aggregateGrainFactoryMock.Setup(f => f.GetGenericAggregate<CoordinatorSagaState>("saga-123"))
            .Returns(grainMock.Object);
        BrookKey brookKey = BrookKey.ForType<CoordinatorSagaState>("saga-123");
        Mock<IBrookCursorGrain> cursorGrainMock = new();
        cursorGrainMock.Setup(g => g.GetLatestPositionAsync()).ReturnsAsync(new BrookPosition(7));
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        brookGrainFactoryMock.Setup(f => f.GetBrookCursorGrain(brookKey)).Returns(cursorGrainMock.Object);
        SnapshotKey snapshotKey = new(
            new(
                brookKey.BrookName,
                SnapshotStorageNameHelper.GetStorageName<SagaRecoveryCheckpoint>(),
                brookKey.EntityId,
                "checkpoint-hash"),
            7);
        SagaRecoveryCheckpoint checkpoint = new()
        {
            PendingDirection = SagaExecutionDirection.Forward,
            PendingStepIndex = 1,
            RecoveryMode = SagaRecoveryMode.Automatic,
            SagaId = Guid.NewGuid(),
            StepHash = ComputeHash(),
        };
        Mock<ISnapshotCacheGrain<SagaRecoveryCheckpoint>> snapshotCacheGrainMock = new();
        snapshotCacheGrainMock.Setup(g => g.GetStateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(checkpoint);
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        snapshotGrainFactoryMock.Setup(f => f.GetSnapshotCacheGrain<SagaRecoveryCheckpoint>(snapshotKey))
            .Returns(snapshotCacheGrainMock.Object);
        SagaRecoveryCoordinator<CoordinatorSagaState> coordinator = CreateCoordinator(
            aggregateGrainFactoryMock,
            brookGrainFactoryMock,
            snapshotGrainFactoryMock);

        SagaRecoveryPlan plan = await coordinator.PlanAsync("saga-123", SagaResumeSource.Manual);

        Assert.Equal(SagaRecoveryPlanDisposition.ExecuteStep, plan.Disposition);
        Assert.Equal(SagaExecutionDirection.Forward, plan.Direction);
        Assert.NotNull(plan.Step);
        Assert.Equal(1, plan.Step.StepIndex);
    }
}