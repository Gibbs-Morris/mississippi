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

using Orleans.Runtime;


namespace Mississippi.DomainModeling.Runtime.L0Tests;

/// <summary>
///     Tests for <see cref="SagaReminderHandler{TSaga}" />.
/// </summary>
public sealed class SagaReminderHandlerTests
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
        string? stepHash = null
    ) =>
        new()
        {
            PendingDirection = direction,
            PendingStepIndex = stepIndex,
            PendingStepName = stepIndex == 0 ? "Debit" : "Credit",
            RecoveryMode = SagaRecoveryMode.Automatic,
            SagaId = Guid.NewGuid(),
            StepHash = stepHash ?? ComputeHash(),
        };

    private static SagaReminderHandler<ReminderSagaState> CreateHandler(
        Mock<IBrookGrainFactory>? brookGrainFactoryMock = null,
        Mock<ISnapshotGrainFactory>? snapshotGrainFactoryMock = null,
        SagaRecoveryOptions? options = null
    )
    {
        brookGrainFactoryMock ??= new();
        snapshotGrainFactoryMock ??= new();
        SagaRecoveryCheckpointAccessor<ReminderSagaState> checkpointAccessor = new(
            brookGrainFactoryMock.Object,
            snapshotGrainFactoryMock.Object);
        Mock<IAggregateGrainFactory> aggregateGrainFactoryMock = new();
        SagaRecoveryCoordinator<ReminderSagaState> recoveryCoordinator = new(
            aggregateGrainFactoryMock.Object,
            checkpointAccessor,
            new(
                new SagaStepInfoProvider<ReminderSagaState>(Steps),
                new SagaRecoveryInfoProvider<ReminderSagaState>(new(SagaRecoveryMode.Automatic, null)),
                Options.Create(options ?? new SagaRecoveryOptions())));
        return new(checkpointAccessor, recoveryCoordinator);
    }

    private static TickStatus CreateTickStatus() =>
        new(
            new(2025, 2, 15, 11, 0, 0, DateTimeKind.Utc),
            TimeSpan.FromMinutes(5),
            new(2025, 2, 15, 11, 5, 0, DateTimeKind.Utc));

    /// <summary>
    ///     Saga state used to exercise reminder handling against a brook-backed saga type.
    /// </summary>
    [BrookName("TEST", "SAGAS", "REMINDER")]
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

    /// <summary>
    ///     Verifies actionable reminder plans execute the internal resume command through the provided delegate.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ReceiveReminderAsyncExecutesResumeCommandForActionableReminder()
    {
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<IBrookCursorGrain> cursorGrainMock = new();
        cursorGrainMock.Setup(c => c.GetLatestPositionAsync()).ReturnsAsync(new BrookPosition(3));
        brookGrainFactoryMock.Setup(f => f.GetBrookCursorGrain(It.IsAny<BrookKey>())).Returns(cursorGrainMock.Object);
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        Mock<ISnapshotCacheGrain<SagaRecoveryCheckpoint>> snapshotCacheGrainMock = new();
        snapshotCacheGrainMock.Setup(s => s.GetStateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateCheckpoint(SagaExecutionDirection.Forward, 0));
        snapshotGrainFactoryMock.Setup(f => f.GetSnapshotCacheGrain<SagaRecoveryCheckpoint>(It.IsAny<SnapshotKey>()))
            .Returns(snapshotCacheGrainMock.Object);
        SagaReminderHandler<ReminderSagaState> handler = CreateHandler(brookGrainFactoryMock, snapshotGrainFactoryMock);
        object? executedCommand = null;
        bool handled = await handler.ReceiveReminderAsync(
            "saga-123",
            SagaReminderNames.Recovery,
            CreateTickStatus(),
            _ => Task.FromResult<ReminderSagaState?>(
                new()
                {
                    Phase = SagaPhase.Running,
                    StepHash = ComputeHash(),
                }),
            (
                command,
                _
            ) =>
            {
                executedCommand = command;
                return Task.FromResult(OperationResult.Ok());
            });
        ResumeSagaCommand command = Assert.IsType<ResumeSagaCommand>(executedCommand);
        Assert.True(handled);
        Assert.Equal(SagaResumeSource.Reminder, command.Source);
        Assert.Equal(SagaRecoveryPlanDisposition.ExecuteStep, command.Disposition);
        Assert.Equal(0, command.StepIndex);
        Assert.Equal("Debit", command.StepName);
    }

    /// <summary>
    ///     Verifies unrelated reminder names are declined without loading saga state.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ReceiveReminderAsyncReturnsFalseForUnknownReminderName()
    {
        SagaReminderHandler<ReminderSagaState> handler = CreateHandler();
        bool stateLoaded = false;
        bool handled = await handler.ReceiveReminderAsync(
            "saga-123",
            "other-reminder",
            CreateTickStatus(),
            cancellationToken =>
            {
                stateLoaded = true;
                return Task.FromResult<ReminderSagaState?>(null);
            },
            (
                _,
                _
            ) => Task.FromResult(OperationResult.Ok()));
        Assert.False(handled);
        Assert.False(stateLoaded);
    }

    /// <summary>
    ///     Verifies missing checkpoints turn into a handled no-op reminder evaluation.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ReceiveReminderAsyncReturnsTrueWhenCheckpointMissing()
    {
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<IBrookCursorGrain> cursorGrainMock = new();
        cursorGrainMock.Setup(c => c.GetLatestPositionAsync()).ReturnsAsync(new BrookPosition());
        brookGrainFactoryMock.Setup(f => f.GetBrookCursorGrain(It.IsAny<BrookKey>())).Returns(cursorGrainMock.Object);
        SagaReminderHandler<ReminderSagaState> handler = CreateHandler(brookGrainFactoryMock);
        bool commandExecuted = false;
        bool handled = await handler.ReceiveReminderAsync(
            "saga-123",
            SagaReminderNames.Recovery,
            CreateTickStatus(),
            _ => Task.FromResult<ReminderSagaState?>(
                new()
                {
                    Phase = SagaPhase.Running,
                    StepHash = ComputeHash(),
                }),
            (
                _,
                _
            ) =>
            {
                commandExecuted = true;
                return Task.FromResult(OperationResult.Ok());
            });
        Assert.True(handled);
        Assert.False(commandExecuted);
    }

    /// <summary>
    ///     Verifies terminal saga state turns into a handled no-op reminder evaluation.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ReceiveReminderAsyncReturnsTrueWhenSagaIsTerminal()
    {
        SagaReminderHandler<ReminderSagaState> handler = CreateHandler();
        bool commandExecuted = false;
        bool handled = await handler.ReceiveReminderAsync(
            "saga-123",
            SagaReminderNames.Recovery,
            CreateTickStatus(),
            _ => Task.FromResult<ReminderSagaState?>(
                new()
                {
                    Phase = SagaPhase.Completed,
                    StepHash = ComputeHash(),
                }),
            (
                _,
                _
            ) =>
            {
                commandExecuted = true;
                return Task.FromResult(OperationResult.Ok());
            });
        Assert.True(handled);
        Assert.False(commandExecuted);
    }

    /// <summary>
    ///     Verifies missing saga state turns into a handled no-op reminder evaluation.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ReceiveReminderAsyncReturnsTrueWhenSagaStateMissing()
    {
        SagaReminderHandler<ReminderSagaState> handler = CreateHandler();
        bool commandExecuted = false;
        bool handled = await handler.ReceiveReminderAsync(
            "saga-123",
            SagaReminderNames.Recovery,
            CreateTickStatus(),
            _ => Task.FromResult<ReminderSagaState?>(null),
            (
                _,
                _
            ) =>
            {
                commandExecuted = true;
                return Task.FromResult(OperationResult.Ok());
            });
        Assert.True(handled);
        Assert.False(commandExecuted);
    }

    /// <summary>
    ///     Verifies workflow mismatches become blocked reminder-driven resume commands so reminders can be disarmed.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ReceiveReminderAsyncReturnsTrueWhenWorkflowMismatchDetected()
    {
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<IBrookCursorGrain> cursorGrainMock = new();
        cursorGrainMock.Setup(c => c.GetLatestPositionAsync()).ReturnsAsync(new BrookPosition(3));
        brookGrainFactoryMock.Setup(f => f.GetBrookCursorGrain(It.IsAny<BrookKey>())).Returns(cursorGrainMock.Object);
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        Mock<ISnapshotCacheGrain<SagaRecoveryCheckpoint>> snapshotCacheGrainMock = new();
        snapshotCacheGrainMock.Setup(s => s.GetStateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateCheckpoint(SagaExecutionDirection.Forward, 0, "DIFFERENT"));
        snapshotGrainFactoryMock.Setup(f => f.GetSnapshotCacheGrain<SagaRecoveryCheckpoint>(It.IsAny<SnapshotKey>()))
            .Returns(snapshotCacheGrainMock.Object);
        SagaReminderHandler<ReminderSagaState> handler = CreateHandler(brookGrainFactoryMock, snapshotGrainFactoryMock);
        object? executedCommand = null;
        bool handled = await handler.ReceiveReminderAsync(
            "saga-123",
            SagaReminderNames.Recovery,
            CreateTickStatus(),
            _ => Task.FromResult<ReminderSagaState?>(
                new()
                {
                    Phase = SagaPhase.Running,
                    StepHash = ComputeHash(),
                }),
            (
                command,
                _
            ) =>
            {
                executedCommand = command;
                return Task.FromResult(OperationResult.Ok());
            });
        ResumeSagaCommand command = Assert.IsType<ResumeSagaCommand>(executedCommand);
        Assert.True(handled);
        Assert.Equal(SagaRecoveryPlanDisposition.Blocked, command.Disposition);
        Assert.Equal(SagaResumeSource.Reminder, command.Source);
        Assert.Equal(0, command.StepIndex);
        Assert.Equal("Debit", command.StepName);
        Assert.Equal("Workflow hash mismatch prevents automatic resume.", command.BlockedReason);
    }

    /// <summary>
    ///     Verifies reminders become handled no-ops when automatic recovery is globally forced into manual-only mode.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ReceiveReminderAsyncReturnsTrueWithoutExecutingCommandWhenAutomaticRecoveryForcedManual()
    {
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        Mock<IBrookCursorGrain> cursorGrainMock = new();
        cursorGrainMock.Setup(c => c.GetLatestPositionAsync()).ReturnsAsync(new BrookPosition(3));
        brookGrainFactoryMock.Setup(f => f.GetBrookCursorGrain(It.IsAny<BrookKey>())).Returns(cursorGrainMock.Object);
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        Mock<ISnapshotCacheGrain<SagaRecoveryCheckpoint>> snapshotCacheGrainMock = new();
        snapshotCacheGrainMock.Setup(s => s.GetStateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateCheckpoint(SagaExecutionDirection.Forward, 0));
        snapshotGrainFactoryMock.Setup(f => f.GetSnapshotCacheGrain<SagaRecoveryCheckpoint>(It.IsAny<SnapshotKey>()))
            .Returns(snapshotCacheGrainMock.Object);
        SagaReminderHandler<ReminderSagaState> handler = CreateHandler(
            brookGrainFactoryMock,
            snapshotGrainFactoryMock,
            new()
            {
                ForceManualOnly = true,
            });
        bool commandExecuted = false;
        bool handled = await handler.ReceiveReminderAsync(
            "saga-123",
            SagaReminderNames.Recovery,
            CreateTickStatus(),
            _ => Task.FromResult<ReminderSagaState?>(
                new()
                {
                    Phase = SagaPhase.Running,
                    StepHash = ComputeHash(),
                }),
            (
                _,
                _
            ) =>
            {
                commandExecuted = true;
                return Task.FromResult(OperationResult.Ok());
            });
        Assert.True(handled);
        Assert.False(commandExecuted);
    }
}