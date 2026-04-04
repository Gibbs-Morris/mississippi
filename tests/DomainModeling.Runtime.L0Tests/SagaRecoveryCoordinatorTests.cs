using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

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
        new(1, "Credit", typeof(SagaNonCompensatableStep), false, SagaStepRecoveryPolicy.ManualOnly, null),
    ];

    private static string ComputeHash() => SagaStepHash.Compute(new(SagaRecoveryMode.Automatic, null), Steps);

    private static SagaRecoveryCheckpoint CreateCheckpoint(
        SagaExecutionDirection direction,
        int stepIndex,
        string? stepHash = null,
        SagaRecoveryMode recoveryMode = SagaRecoveryMode.Automatic
    ) =>
        new()
        {
            PendingDirection = direction,
            PendingStepIndex = stepIndex,
            RecoveryMode = recoveryMode,
            SagaId = Guid.NewGuid(),
            StepHash = stepHash ?? ComputeHash(),
        };

    private static SagaRecoveryCoordinator<CoordinatorSagaState> CreateCoordinator(
        Mock<IAggregateGrainFactory> aggregateGrainFactoryMock,
        Mock<IBrookGrainFactory>? brookGrainFactoryMock = null,
        Mock<ISnapshotGrainFactory>? snapshotGrainFactoryMock = null,
        SagaRecoveryOptions? options = null
    )
    {
        brookGrainFactoryMock ??= new();
        snapshotGrainFactoryMock ??= new();
        return new(
            aggregateGrainFactoryMock.Object,
            new(brookGrainFactoryMock.Object, snapshotGrainFactoryMock.Object),
            new(
                new SagaStepInfoProvider<CoordinatorSagaState>(Steps),
                new SagaRecoveryInfoProvider<CoordinatorSagaState>(new(SagaRecoveryMode.Automatic, null)),
                Options.Create(options ?? new SagaRecoveryOptions())));
    }

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

    /// <summary>
    ///     Verifies reminder planning becomes a no-op when automatic recovery is globally forced into manual-only mode.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task PlanAsyncReturnsNoActionWhenAutomaticRecoveryForcedManual()
    {
        Mock<IGenericAggregateGrain<CoordinatorSagaState>> grainMock = new();
        grainMock.Setup(g => g.GetStateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new CoordinatorSagaState
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
                SagaRecoveryCheckpointMetadata.CheckpointReducerHash),
            7);
        Mock<ISnapshotCacheGrain<SagaRecoveryCheckpoint>> snapshotCacheGrainMock = new();
        snapshotCacheGrainMock.Setup(g => g.GetStateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateCheckpoint(SagaExecutionDirection.Forward, 0));
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        snapshotGrainFactoryMock.Setup(f => f.GetSnapshotCacheGrain<SagaRecoveryCheckpoint>(snapshotKey))
            .Returns(snapshotCacheGrainMock.Object);
        SagaRecoveryCoordinator<CoordinatorSagaState> coordinator = CreateCoordinator(
            aggregateGrainFactoryMock,
            brookGrainFactoryMock,
            snapshotGrainFactoryMock,
            new()
            {
                ForceManualOnly = true,
            });
        SagaRecoveryPlan plan = await coordinator.PlanAsync("saga-123", SagaResumeSource.Reminder);
        Assert.Equal(SagaRecoveryPlanDisposition.NoAction, plan.Disposition);
    }

    /// <summary>
    ///     Verifies missing checkpoints conservatively produce a no-action plan.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task PlanAsyncReturnsNoActionWhenCheckpointMissing()
    {
        Mock<IGenericAggregateGrain<CoordinatorSagaState>> grainMock = new();
        grainMock.Setup(g => g.GetStateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new CoordinatorSagaState
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
            brookGrainFactoryMock);
        SagaRecoveryPlan plan = await coordinator.PlanAsync("saga-123", SagaResumeSource.Reminder);
        Assert.Equal(SagaRecoveryPlanDisposition.NoAction, plan.Disposition);
        Assert.Equal("Recovery checkpoint not found.", plan.Reason);
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
            brookGrainFactoryMock);
        SagaRecoveryPlan plan = await coordinator.PlanAsync("saga-123", SagaResumeSource.Reminder);
        Assert.Equal(SagaRecoveryPlanDisposition.NoAction, plan.Disposition);
        Assert.Equal("Saga state not found.", plan.Reason);
        brookGrainFactoryMock.Verify(f => f.GetBrookCursorGrain(It.IsAny<BrookKey>()), Times.Never);
    }

    /// <summary>
    ///     Verifies manual sources can still execute manual-only steps through the delegated planner.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task PlanAsyncReturnsPlannerDecisionForManualResume()
    {
        Mock<IGenericAggregateGrain<CoordinatorSagaState>> grainMock = new();
        grainMock.Setup(g => g.GetStateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new CoordinatorSagaState
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
                SagaRecoveryCheckpointMetadata.CheckpointReducerHash),
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

    /// <summary>
    ///     Verifies reminder planning delegates to the recovery planner once state and checkpoint are loaded.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task PlanAsyncReturnsPlannerDecisionForReminder()
    {
        Mock<IGenericAggregateGrain<CoordinatorSagaState>> grainMock = new();
        grainMock.Setup(g => g.GetStateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new CoordinatorSagaState
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
                SagaRecoveryCheckpointMetadata.CheckpointReducerHash),
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
    ///     Verifies terminal saga state short-circuits to a terminal plan without loading checkpoints.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task PlanAsyncReturnsTerminalWhenSagaAlreadyTerminal()
    {
        Mock<IGenericAggregateGrain<CoordinatorSagaState>> grainMock = new();
        grainMock.Setup(g => g.GetStateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new CoordinatorSagaState
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
            brookGrainFactoryMock);
        SagaRecoveryPlan plan = await coordinator.PlanAsync("saga-123", SagaResumeSource.Reminder);
        Assert.Equal(SagaRecoveryPlanDisposition.Terminal, plan.Disposition);
        brookGrainFactoryMock.Verify(f => f.GetBrookCursorGrain(It.IsAny<BrookKey>()), Times.Never);
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
        await Assert.ThrowsAnyAsync<ArgumentException>(() => coordinator.PlanAsync(
            entityId!,
            SagaResumeSource.Reminder));
    }

    /// <summary>
    ///     Verifies resume execution emits a blocked resume command for reminder-driven manual-only work.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task ResumeAsyncExecutesBlockedCommandForReminderPlan()
    {
        ResumeSagaCommand? capturedCommand = null;
        Mock<IGenericAggregateGrain<CoordinatorSagaState>> grainMock = new();
        grainMock.Setup(g => g.GetStateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new CoordinatorSagaState
                {
                    Phase = SagaPhase.Running,
                    SagaId = Guid.NewGuid(),
                    StepHash = ComputeHash(),
                });
        grainMock.Setup(g => g.ExecuteAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((
                command,
                _
            ) => capturedCommand = Assert.IsType<ResumeSagaCommand>(command))
            .ReturnsAsync(OperationResult.Ok());
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
                SagaRecoveryCheckpointMetadata.CheckpointReducerHash),
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
        OperationResult<SagaRecoveryPlan> result = await coordinator.ResumeAsync("saga-123", SagaResumeSource.Reminder);
        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Equal(SagaRecoveryPlanDisposition.Blocked, result.Value.Disposition);
        Assert.NotNull(capturedCommand);
        Assert.Equal(SagaRecoveryPlanDisposition.Blocked, capturedCommand.Disposition);
        Assert.Equal(SagaResumeSource.Reminder, capturedCommand.Source);
        Assert.Equal(SagaExecutionDirection.Forward, capturedCommand.Direction);
        Assert.Equal(1, capturedCommand.StepIndex);
        Assert.Equal("Credit", capturedCommand.StepName);
        Assert.Equal("Step 'Credit' requires manual forward recovery.", capturedCommand.BlockedReason);
    }

    /// <summary>
    ///     Verifies compensation-complete plans execute a terminal compensation command.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task ResumeAsyncExecutesCompensateSagaCommand()
    {
        ResumeSagaCommand? capturedCommand = null;
        Mock<IGenericAggregateGrain<CoordinatorSagaState>> grainMock = new();
        grainMock.Setup(g => g.GetStateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new CoordinatorSagaState
                {
                    Phase = SagaPhase.Running,
                    SagaId = Guid.NewGuid(),
                    StepHash = ComputeHash(),
                });
        grainMock.Setup(g => g.ExecuteAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((
                command,
                _
            ) => capturedCommand = Assert.IsType<ResumeSagaCommand>(command))
            .ReturnsAsync(OperationResult.Ok());
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
                SagaRecoveryCheckpointMetadata.CheckpointReducerHash),
            7);
        SagaRecoveryCheckpoint checkpoint = CreateCheckpoint(SagaExecutionDirection.Compensation, -1);
        Mock<ISnapshotCacheGrain<SagaRecoveryCheckpoint>> snapshotCacheGrainMock = new();
        snapshotCacheGrainMock.Setup(g => g.GetStateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(checkpoint);
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        snapshotGrainFactoryMock.Setup(f => f.GetSnapshotCacheGrain<SagaRecoveryCheckpoint>(snapshotKey))
            .Returns(snapshotCacheGrainMock.Object);
        SagaRecoveryCoordinator<CoordinatorSagaState> coordinator = CreateCoordinator(
            aggregateGrainFactoryMock,
            brookGrainFactoryMock,
            snapshotGrainFactoryMock);
        OperationResult<SagaRecoveryPlan> result = await coordinator.ResumeAsync("saga-123", SagaResumeSource.Reminder);
        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Equal(SagaRecoveryPlanDisposition.CompensateSaga, result.Value.Disposition);
        Assert.NotNull(capturedCommand);
        Assert.Equal(SagaRecoveryPlanDisposition.CompensateSaga, capturedCommand.Disposition);
        Assert.Equal(SagaResumeSource.Reminder, capturedCommand.Source);
        Assert.Null(capturedCommand.StepIndex);
        Assert.Null(capturedCommand.StepName);
    }

    /// <summary>
    ///     Verifies forward-complete plans execute a terminal completion command.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task ResumeAsyncExecutesCompleteSagaCommand()
    {
        ResumeSagaCommand? capturedCommand = null;
        Mock<IGenericAggregateGrain<CoordinatorSagaState>> grainMock = new();
        grainMock.Setup(g => g.GetStateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new CoordinatorSagaState
                {
                    Phase = SagaPhase.Running,
                    SagaId = Guid.NewGuid(),
                    StepHash = ComputeHash(),
                });
        grainMock.Setup(g => g.ExecuteAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((
                command,
                _
            ) => capturedCommand = Assert.IsType<ResumeSagaCommand>(command))
            .ReturnsAsync(OperationResult.Ok());
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
                SagaRecoveryCheckpointMetadata.CheckpointReducerHash),
            7);
        SagaRecoveryCheckpoint checkpoint = CreateCheckpoint(SagaExecutionDirection.Forward, 2);
        Mock<ISnapshotCacheGrain<SagaRecoveryCheckpoint>> snapshotCacheGrainMock = new();
        snapshotCacheGrainMock.Setup(g => g.GetStateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(checkpoint);
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        snapshotGrainFactoryMock.Setup(f => f.GetSnapshotCacheGrain<SagaRecoveryCheckpoint>(snapshotKey))
            .Returns(snapshotCacheGrainMock.Object);
        SagaRecoveryCoordinator<CoordinatorSagaState> coordinator = CreateCoordinator(
            aggregateGrainFactoryMock,
            brookGrainFactoryMock,
            snapshotGrainFactoryMock);
        OperationResult<SagaRecoveryPlan> result = await coordinator.ResumeAsync("saga-123", SagaResumeSource.Manual);
        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Equal(SagaRecoveryPlanDisposition.CompleteSaga, result.Value.Disposition);
        Assert.NotNull(capturedCommand);
        Assert.Equal(SagaRecoveryPlanDisposition.CompleteSaga, capturedCommand.Disposition);
        Assert.Equal(SagaResumeSource.Manual, capturedCommand.Source);
        Assert.Null(capturedCommand.StepIndex);
        Assert.Null(capturedCommand.StepName);
    }

    /// <summary>
    ///     Verifies resume command failures propagate as failed coordinator results.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task ResumeAsyncReturnsFailureWhenCommandExecutionFails()
    {
        Mock<IGenericAggregateGrain<CoordinatorSagaState>> grainMock = new();
        grainMock.Setup(g => g.GetStateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new CoordinatorSagaState
                {
                    Phase = SagaPhase.Running,
                    SagaId = Guid.NewGuid(),
                    StepHash = ComputeHash(),
                });
        grainMock.Setup(g => g.ExecuteAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult.Fail(AggregateErrorCodes.InvalidState, "resume failed"));
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
                SagaRecoveryCheckpointMetadata.CheckpointReducerHash),
            7);
        SagaRecoveryCheckpoint checkpoint = CreateCheckpoint(SagaExecutionDirection.Compensation, -1);
        Mock<ISnapshotCacheGrain<SagaRecoveryCheckpoint>> snapshotCacheGrainMock = new();
        snapshotCacheGrainMock.Setup(g => g.GetStateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(checkpoint);
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        snapshotGrainFactoryMock.Setup(f => f.GetSnapshotCacheGrain<SagaRecoveryCheckpoint>(snapshotKey))
            .Returns(snapshotCacheGrainMock.Object);
        SagaRecoveryCoordinator<CoordinatorSagaState> coordinator = CreateCoordinator(
            aggregateGrainFactoryMock,
            brookGrainFactoryMock,
            snapshotGrainFactoryMock);
        OperationResult<SagaRecoveryPlan> result = await coordinator.ResumeAsync("saga-123", SagaResumeSource.Reminder);
        Assert.False(result.Success);
        Assert.Equal(AggregateErrorCodes.InvalidState, result.ErrorCode);
        Assert.Equal("resume failed", result.ErrorMessage);
    }

    /// <summary>
    ///     Verifies resume execution returns no action when the checkpoint is missing.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task ResumeAsyncReturnsNoActionWhenCheckpointMissing()
    {
        Mock<IGenericAggregateGrain<CoordinatorSagaState>> grainMock = new();
        grainMock.Setup(g => g.GetStateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new CoordinatorSagaState
                {
                    Phase = SagaPhase.Running,
                    StepHash = ComputeHash(),
                });
        Mock<IAggregateGrainFactory> aggregateGrainFactoryMock = new();
        aggregateGrainFactoryMock.Setup(f => f.GetGenericAggregate<CoordinatorSagaState>("saga-123"))
            .Returns(grainMock.Object);
        Mock<IBrookCursorGrain> cursorGrainMock = new();
        cursorGrainMock.Setup(g => g.GetLatestPositionAsync()).ReturnsAsync(new BrookPosition(7));
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        brookGrainFactoryMock.Setup(f => f.GetBrookCursorGrain(It.IsAny<BrookKey>())).Returns(cursorGrainMock.Object);
        BrookKey brookKey = BrookKey.ForType<CoordinatorSagaState>("saga-123");
        SnapshotKey snapshotKey = new(
            new(
                brookKey.BrookName,
                SnapshotStorageNameHelper.GetStorageName<SagaRecoveryCheckpoint>(),
                brookKey.EntityId,
                SagaRecoveryCheckpointMetadata.CheckpointReducerHash),
            7);
        Mock<ISnapshotCacheGrain<SagaRecoveryCheckpoint>> snapshotCacheGrainMock = new();
        snapshotCacheGrainMock.Setup(g => g.GetStateAsync(It.IsAny<CancellationToken>()))
            .Returns(() => ValueTask.FromResult((SagaRecoveryCheckpoint)null!));
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        snapshotGrainFactoryMock.Setup(f => f.GetSnapshotCacheGrain<SagaRecoveryCheckpoint>(snapshotKey))
            .Returns(snapshotCacheGrainMock.Object);
        SagaRecoveryCoordinator<CoordinatorSagaState> coordinator = CreateCoordinator(
            aggregateGrainFactoryMock,
            brookGrainFactoryMock,
            snapshotGrainFactoryMock);
        OperationResult<SagaRecoveryPlan> result = await coordinator.ResumeAsync("saga-123", SagaResumeSource.Manual);
        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Equal(SagaRecoveryPlanDisposition.NoAction, result.Value.Disposition);
        grainMock.Verify(g => g.ExecuteAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    ///     Verifies reminder resumes short-circuit when saga recovery mode is manual-only.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task ResumeAsyncReturnsNoActionWhenReminderHitsManualOnlySaga()
    {
        Mock<IGenericAggregateGrain<CoordinatorSagaState>> grainMock = new();
        grainMock.Setup(g => g.GetStateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new CoordinatorSagaState
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
                SagaRecoveryCheckpointMetadata.CheckpointReducerHash),
            7);
        SagaRecoveryCheckpoint checkpoint = CreateCheckpoint(
            SagaExecutionDirection.Forward,
            0,
            recoveryMode: SagaRecoveryMode.ManualOnly);
        Mock<ISnapshotCacheGrain<SagaRecoveryCheckpoint>> snapshotCacheGrainMock = new();
        snapshotCacheGrainMock.Setup(g => g.GetStateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(checkpoint);
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        snapshotGrainFactoryMock.Setup(f => f.GetSnapshotCacheGrain<SagaRecoveryCheckpoint>(snapshotKey))
            .Returns(snapshotCacheGrainMock.Object);
        SagaRecoveryCoordinator<CoordinatorSagaState> coordinator = CreateCoordinator(
            aggregateGrainFactoryMock,
            brookGrainFactoryMock,
            snapshotGrainFactoryMock);
        OperationResult<SagaRecoveryPlan> result = await coordinator.ResumeAsync("saga-123", SagaResumeSource.Reminder);
        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Equal(SagaRecoveryPlanDisposition.NoAction, result.Value.Disposition);
        grainMock.Verify(g => g.ExecuteAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    ///     Verifies resume execution returns no action when the saga state is missing.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task ResumeAsyncReturnsNoActionWhenSagaStateMissing()
    {
        Mock<IGenericAggregateGrain<CoordinatorSagaState>> grainMock = new();
        grainMock.Setup(g => g.GetStateAsync(It.IsAny<CancellationToken>())).ReturnsAsync((CoordinatorSagaState?)null);
        Mock<IAggregateGrainFactory> aggregateGrainFactoryMock = new();
        aggregateGrainFactoryMock.Setup(f => f.GetGenericAggregate<CoordinatorSagaState>("saga-123"))
            .Returns(grainMock.Object);
        SagaRecoveryCoordinator<CoordinatorSagaState> coordinator = CreateCoordinator(aggregateGrainFactoryMock);
        OperationResult<SagaRecoveryPlan> result = await coordinator.ResumeAsync("saga-123", SagaResumeSource.Reminder);
        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Equal(SagaRecoveryPlanDisposition.NoAction, result.Value.Disposition);
        grainMock.Verify(g => g.ExecuteAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    ///     Verifies resume execution returns terminal when the saga has already finished.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task ResumeAsyncReturnsTerminalWhenSagaAlreadyTerminal()
    {
        Mock<IGenericAggregateGrain<CoordinatorSagaState>> grainMock = new();
        grainMock.Setup(g => g.GetStateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new CoordinatorSagaState
                {
                    Phase = SagaPhase.Compensated,
                    StepHash = ComputeHash(),
                });
        Mock<IAggregateGrainFactory> aggregateGrainFactoryMock = new();
        aggregateGrainFactoryMock.Setup(f => f.GetGenericAggregate<CoordinatorSagaState>("saga-123"))
            .Returns(grainMock.Object);
        SagaRecoveryCoordinator<CoordinatorSagaState> coordinator = CreateCoordinator(aggregateGrainFactoryMock);
        OperationResult<SagaRecoveryPlan> result = await coordinator.ResumeAsync("saga-123", SagaResumeSource.Manual);
        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Equal(SagaRecoveryPlanDisposition.Terminal, result.Value.Disposition);
        grainMock.Verify(g => g.ExecuteAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    ///     Verifies workflow mismatches short-circuit without issuing a resume command.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task ResumeAsyncReturnsWorkflowMismatchWithoutExecutingCommand()
    {
        Mock<IGenericAggregateGrain<CoordinatorSagaState>> grainMock = new();
        grainMock.Setup(g => g.GetStateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new CoordinatorSagaState
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
                SagaRecoveryCheckpointMetadata.CheckpointReducerHash),
            7);
        SagaRecoveryCheckpoint checkpoint = CreateCheckpoint(SagaExecutionDirection.Forward, 0, "DIFFERENT-HASH");
        Mock<ISnapshotCacheGrain<SagaRecoveryCheckpoint>> snapshotCacheGrainMock = new();
        snapshotCacheGrainMock.Setup(g => g.GetStateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(checkpoint);
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        snapshotGrainFactoryMock.Setup(f => f.GetSnapshotCacheGrain<SagaRecoveryCheckpoint>(snapshotKey))
            .Returns(snapshotCacheGrainMock.Object);
        SagaRecoveryCoordinator<CoordinatorSagaState> coordinator = CreateCoordinator(
            aggregateGrainFactoryMock,
            brookGrainFactoryMock,
            snapshotGrainFactoryMock);
        OperationResult<SagaRecoveryPlan> result = await coordinator.ResumeAsync("saga-123", SagaResumeSource.Manual);
        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Equal(SagaRecoveryPlanDisposition.WorkflowMismatch, result.Value.Disposition);
        grainMock.Verify(g => g.ExecuteAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    ///     Verifies resume execution reuses the persisted in-flight identity for replayable work.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task ResumeAsyncReusesInFlightIdentityForExecutePlan()
    {
        ResumeSagaCommand? capturedCommand = null;
        Guid attemptId = Guid.NewGuid();
        Mock<IGenericAggregateGrain<CoordinatorSagaState>> grainMock = new();
        grainMock.Setup(g => g.GetStateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new CoordinatorSagaState
                {
                    Phase = SagaPhase.Running,
                    SagaId = Guid.NewGuid(),
                    StepHash = ComputeHash(),
                });
        grainMock.Setup(g => g.ExecuteAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((
                command,
                _
            ) => capturedCommand = Assert.IsType<ResumeSagaCommand>(command))
            .ReturnsAsync(OperationResult.Ok());
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
                SagaRecoveryCheckpointMetadata.CheckpointReducerHash),
            7);
        SagaRecoveryCheckpoint checkpoint = new()
        {
            InFlightAttemptId = attemptId,
            InFlightOperationKey = "resume-op",
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
        OperationResult<SagaRecoveryPlan> result = await coordinator.ResumeAsync("saga-123", SagaResumeSource.Manual);
        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Equal(SagaRecoveryPlanDisposition.ExecuteStep, result.Value.Disposition);
        Assert.NotNull(capturedCommand);
        Assert.Equal(SagaRecoveryPlanDisposition.ExecuteStep, capturedCommand.Disposition);
        Assert.Equal(SagaResumeSource.Manual, capturedCommand.Source);
        Assert.Equal(attemptId, capturedCommand.AttemptId);
        Assert.Equal("resume-op", capturedCommand.OperationKey);
        Assert.Equal(1, capturedCommand.StepIndex);
        Assert.Equal("Credit", capturedCommand.StepName);
    }
}