using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Abstractions.Attributes;
using Mississippi.Brooks.Abstractions.Cursor;
using Mississippi.Brooks.Abstractions.Factory;
using Mississippi.DomainModeling.Abstractions;
using Mississippi.Tributary.Abstractions;

using Moq;


namespace Mississippi.DomainModeling.Runtime.L0Tests;

/// <summary>
///     Tests for <see cref="SagaRecoveryCheckpointAccessor{TSaga}" />.
/// </summary>
public sealed class SagaRecoveryCheckpointAccessorTests
{
    [BrookName("TEST", "SAGAS", "ACCESSOR")]
    private sealed record AccessorSagaState : ISagaState
    {
        public string? CorrelationId { get; init; }

        public int LastCompletedStepIndex { get; init; } = -1;

        public SagaPhase Phase { get; init; }

        public Guid SagaId { get; init; }

        public DateTimeOffset? StartedAt { get; init; }

        public string? StepHash { get; init; }
    }

    private sealed record TestInput(string Value);

    private static SagaRecoveryCheckpointAccessor<AccessorSagaState> CreateAccessor(
        Mock<IBrookGrainFactory>? brookGrainFactoryMock = null,
        Mock<ISnapshotGrainFactory>? snapshotGrainFactoryMock = null,
        Mock<IRootReducer<SagaRecoveryCheckpoint>>? checkpointReducerMock = null
    )
    {
        brookGrainFactoryMock ??= new();
        snapshotGrainFactoryMock ??= new();
        checkpointReducerMock ??= new();
        checkpointReducerMock.Setup(r => r.GetReducerHash()).Returns("checkpoint-hash");
        return new(
            brookGrainFactoryMock.Object,
            snapshotGrainFactoryMock.Object,
            checkpointReducerMock.Object);
    }

    /// <summary>
    ///     Verifies saga orchestration registration exposes the checkpoint accessor.
    /// </summary>
    [Fact]
    public void AddSagaOrchestrationRegistersRecoveryCheckpointAccessor()
    {
        ServiceCollection services = new();
        services.AddSagaOrchestration<TestSagaState, TestInput>();

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(SagaRecoveryCheckpointAccessor<TestSagaState>)
                          && descriptor.ImplementationType == typeof(SagaRecoveryCheckpointAccessor<TestSagaState>)
                          && descriptor.Lifetime == ServiceLifetime.Transient);
    }

    /// <summary>
    ///     Verifies null or whitespace entity identifiers are rejected.
    /// </summary>
    /// <param name="entityId">The invalid entity identifier to test.</param>
    /// <returns>A task that represents the asynchronous assertion.</returns>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetAsyncThrowsWhenEntityIdInvalid(
        string? entityId
    )
    {
        SagaRecoveryCheckpointAccessor<AccessorSagaState> accessor = CreateAccessor();

        await Assert.ThrowsAnyAsync<ArgumentException>(() => accessor.GetAsync(entityId!));
    }

    /// <summary>
    ///     Verifies no snapshot is loaded when the saga brook has not written any events.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task GetAsyncReturnsNullWhenBrookPositionNotSet()
    {
        Mock<IBrookCursorGrain> cursorGrainMock = new();
        cursorGrainMock.Setup(g => g.GetLatestPositionAsync()).ReturnsAsync(new BrookPosition());
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        brookGrainFactoryMock.Setup(f => f.GetBrookCursorGrain(It.IsAny<BrookKey>())).Returns(cursorGrainMock.Object);
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        SagaRecoveryCheckpointAccessor<AccessorSagaState> accessor = CreateAccessor(
            brookGrainFactoryMock: brookGrainFactoryMock,
            snapshotGrainFactoryMock: snapshotGrainFactoryMock);

        SagaRecoveryCheckpoint? result = await accessor.GetAsync("saga-123");

        Assert.Null(result);
        snapshotGrainFactoryMock.Verify(
            f => f.GetSnapshotCacheGrain<SagaRecoveryCheckpoint>(It.IsAny<SnapshotKey>()),
            Times.Never);
    }

    /// <summary>
    ///     Verifies the accessor loads the latest checkpoint snapshot using the checkpoint reducer hash.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Fact]
    public async Task GetAsyncLoadsCheckpointFromLatestSnapshotVersion()
    {
        CancellationToken cancellationToken = new(false);
        BrookKey expectedBrookKey = BrookKey.ForType<AccessorSagaState>("saga-123");
        Mock<IBrookCursorGrain> cursorGrainMock = new();
        cursorGrainMock.Setup(g => g.GetLatestPositionAsync()).ReturnsAsync(new BrookPosition(42));
        Mock<IBrookGrainFactory> brookGrainFactoryMock = new();
        brookGrainFactoryMock.Setup(f => f.GetBrookCursorGrain(expectedBrookKey)).Returns(cursorGrainMock.Object);
        Mock<IRootReducer<SagaRecoveryCheckpoint>> checkpointReducerMock = new();
        checkpointReducerMock.Setup(r => r.GetReducerHash()).Returns("checkpoint-hash");
        SnapshotKey expectedSnapshotKey = new(
            new(
                expectedBrookKey.BrookName,
                SnapshotStorageNameHelper.GetStorageName<SagaRecoveryCheckpoint>(),
                expectedBrookKey.EntityId,
                "checkpoint-hash"),
            42);
        SagaRecoveryCheckpoint expectedCheckpoint = new()
        {
            SagaId = Guid.NewGuid(),
            StepHash = "HASH",
            RecoveryMode = SagaRecoveryMode.Automatic,
        };
        Mock<ISnapshotCacheGrain<SagaRecoveryCheckpoint>> snapshotCacheGrainMock = new();
        snapshotCacheGrainMock.Setup(g => g.GetStateAsync(cancellationToken)).ReturnsAsync(expectedCheckpoint);
        Mock<ISnapshotGrainFactory> snapshotGrainFactoryMock = new();
        snapshotGrainFactoryMock.Setup(f => f.GetSnapshotCacheGrain<SagaRecoveryCheckpoint>(expectedSnapshotKey))
            .Returns(snapshotCacheGrainMock.Object);
        SagaRecoveryCheckpointAccessor<AccessorSagaState> accessor = CreateAccessor(
            brookGrainFactoryMock,
            snapshotGrainFactoryMock,
            checkpointReducerMock);

        SagaRecoveryCheckpoint? result = await accessor.GetAsync("saga-123", cancellationToken);

        Assert.Same(expectedCheckpoint, result);
        snapshotCacheGrainMock.Verify(g => g.GetStateAsync(cancellationToken), Times.Once);
    }
}