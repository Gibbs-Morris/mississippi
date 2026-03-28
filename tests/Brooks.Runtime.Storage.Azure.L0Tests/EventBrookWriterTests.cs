using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Runtime.Storage.Azure.Brooks;
using Mississippi.Brooks.Runtime.Storage.Azure.Locking;
using Mississippi.Brooks.Runtime.Storage.Azure.Storage;

using Moq;


namespace Mississippi.Brooks.Runtime.Storage.Azure.L0Tests;

/// <summary>
///     Tests for <see cref="EventBrookWriter" />.
/// </summary>
public sealed class EventBrookWriterTests
{
    /// <summary>
    ///     Append rejects stale expected versions.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task AppendEventsAsyncThrowsOptimisticConcurrencyExceptionWhenExpectedVersionDoesNotMatch()
    {
        Mock<IAzureBrookRepository> repository = new(MockBehavior.Strict);
        Mock<IDistributedLockManager> lockManager = new(MockBehavior.Strict);
        Mock<IBrookRecoveryService> recoveryService = new(MockBehavior.Strict);
        Mock<IDistributedLock> distributedLock = new(MockBehavior.Strict);
        BrookKey brookId = new("orders", "123");

        distributedLock.SetupGet(item => item.LockId).Returns("lease-1");
        distributedLock.Setup(item => item.DisposeAsync()).Returns(default(ValueTask));
        lockManager.Setup(manager => manager.AcquireLockAsync(
                brookId,
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(distributedLock.Object);
        recoveryService.Setup(service => service.GetOrRecoverCursorPositionAsync(
                brookId,
                distributedLock.Object,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrookPosition(5));

        EventBrookWriter writer = new(
            repository.Object,
            lockManager.Object,
            recoveryService.Object,
            Options.Create(new BrookStorageOptions()));

        await Assert.ThrowsAsync<OptimisticConcurrencyException>(() => writer.AppendEventsAsync(
            brookId,
            [CreateEvent("e-1")],
            new BrookPosition(4)));
    }

    /// <summary>
    ///     Append writes pending state, uploads events, and commits the cursor.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task AppendEventsAsyncWritesPendingEventsAndCommits()
    {
        Mock<IAzureBrookRepository> repository = new(MockBehavior.Strict);
        Mock<IDistributedLockManager> lockManager = new(MockBehavior.Strict);
        Mock<IBrookRecoveryService> recoveryService = new(MockBehavior.Strict);
        Mock<IDistributedLock> distributedLock = new(MockBehavior.Strict);
        BrookKey brookId = new("orders", "123");
        AzureBrookCommittedCursorState cursor = new()
        {
            Position = 1,
        };

        distributedLock.SetupGet(item => item.LockId).Returns("lease-1");
        distributedLock.Setup(item => item.DisposeAsync()).Returns(default(ValueTask));
        distributedLock.Setup(item => item.RenewAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        lockManager.Setup(manager => manager.AcquireLockAsync(
                brookId,
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(distributedLock.Object);
        recoveryService.Setup(service => service.GetOrRecoverCursorPositionAsync(
                brookId,
                distributedLock.Object,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrookPosition(1));
        repository.Setup(repo => repo.TryCreatePendingWriteAsync(
                brookId,
                It.Is<AzureBrookPendingWriteState>(state =>
                    (state.OriginalPosition == 1) &&
                    (state.TargetPosition == 3) &&
                    (state.WriteEpoch == 3) &&
                    (state.EventCount == 2)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repository.Setup(repo => repo.WriteEventAsync(brookId, 2, It.IsAny<BrookEvent>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        repository.Setup(repo => repo.WriteEventAsync(brookId, 3, It.IsAny<BrookEvent>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        repository.Setup(repo => repo.GetCommittedCursorAsync(brookId, It.IsAny<CancellationToken>())).ReturnsAsync(cursor);
        repository.Setup(repo => repo.TryAdvanceCommittedCursorAsync(
                brookId,
                cursor,
                It.Is<AzureBrookPendingWriteState>(state => state.TargetPosition == 3),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repository.Setup(repo => repo.TryDeletePendingWriteAsync(
                brookId,
                It.Is<AzureBrookPendingWriteState>(state => state.TargetPosition == 3),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        EventBrookWriter writer = new(
            repository.Object,
            lockManager.Object,
            recoveryService.Object,
            Options.Create(new BrookStorageOptions()));

        BrookPosition result = await writer.AppendEventsAsync(
            brookId,
            [CreateEvent("e-1"), CreateEvent("e-2")],
            new BrookPosition(1));

        Assert.Equal(3, result.Value);
        repository.VerifyAll();
        recoveryService.VerifyAll();
        lockManager.VerifyAll();
    }

    /// <summary>
    ///     Append stays retryable when another writer already owns pending state.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task AppendEventsAsyncThrowsRetryableExceptionWhenPendingStateAlreadyExists()
    {
        Mock<IAzureBrookRepository> repository = new(MockBehavior.Strict);
        Mock<IDistributedLockManager> lockManager = new(MockBehavior.Strict);
        Mock<IBrookRecoveryService> recoveryService = new(MockBehavior.Strict);
        Mock<IDistributedLock> distributedLock = new(MockBehavior.Strict);
        BrookKey brookId = new("orders", "123");

        distributedLock.SetupGet(item => item.LockId).Returns("lease-1");
        distributedLock.Setup(item => item.DisposeAsync()).Returns(default(ValueTask));
        lockManager.Setup(manager => manager.AcquireLockAsync(
                brookId,
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(distributedLock.Object);
        recoveryService.Setup(service => service.GetOrRecoverCursorPositionAsync(
                brookId,
                distributedLock.Object,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrookPosition(1));
        repository.Setup(repo => repo.TryCreatePendingWriteAsync(
                brookId,
                It.Is<AzureBrookPendingWriteState>(state =>
                    (state.OriginalPosition == 1) &&
                    (state.TargetPosition == 2) &&
                    (state.EventCount == 1)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        EventBrookWriter writer = new(
            repository.Object,
            lockManager.Object,
            recoveryService.Object,
            Options.Create(new BrookStorageOptions()));

        BrookStorageRetryableException exception = await Assert.ThrowsAsync<BrookStorageRetryableException>(() => writer.AppendEventsAsync(
            brookId,
            [CreateEvent("e-1")],
            new BrookPosition(1)));

        Assert.Contains(brookId.ToString(), exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Append reports an ambiguous outcome when a stale writer loses commit ownership and reconciliation stays behind target.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task AppendEventsAsyncThrowsAmbiguousOutcomeExceptionWhenCommitConflictCannotBeReconciled()
    {
        Mock<IAzureBrookRepository> repository = new(MockBehavior.Strict);
        Mock<IDistributedLockManager> lockManager = new(MockBehavior.Strict);
        Mock<IBrookRecoveryService> recoveryService = new(MockBehavior.Strict);
        Mock<IDistributedLock> distributedLock = new(MockBehavior.Strict);
        BrookKey brookId = new("orders", "123");
        AzureBrookCommittedCursorState cursor = new()
        {
            Position = 1,
        };

        distributedLock.SetupGet(item => item.LockId).Returns("lease-1");
        distributedLock.Setup(item => item.DisposeAsync()).Returns(default(ValueTask));
        distributedLock.Setup(item => item.RenewAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        lockManager.Setup(manager => manager.AcquireLockAsync(
                brookId,
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(distributedLock.Object);
        recoveryService.SetupSequence(service => service.GetOrRecoverCursorPositionAsync(
                brookId,
                distributedLock.Object,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrookPosition(1))
            .ReturnsAsync(new BrookPosition(1));
        repository.Setup(repo => repo.TryCreatePendingWriteAsync(
                brookId,
                It.IsAny<AzureBrookPendingWriteState>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repository.Setup(repo => repo.WriteEventAsync(brookId, 2, It.IsAny<BrookEvent>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        repository.Setup(repo => repo.GetCommittedCursorAsync(brookId, It.IsAny<CancellationToken>())).ReturnsAsync(cursor);
        repository.Setup(repo => repo.TryAdvanceCommittedCursorAsync(
                brookId,
                cursor,
                It.Is<AzureBrookPendingWriteState>(state => state.TargetPosition == 2),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        EventBrookWriter writer = new(
            repository.Object,
            lockManager.Object,
            recoveryService.Object,
            Options.Create(new BrookStorageOptions()));

        BrookStorageAmbiguousOutcomeException exception = await Assert.ThrowsAsync<BrookStorageAmbiguousOutcomeException>(() => writer.AppendEventsAsync(
            brookId,
            [CreateEvent("e-1")],
            new BrookPosition(1)));

        Assert.Contains("Re-read committed state", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Append returns success when recovery proves an unknown outcome committed.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task AppendEventsAsyncReturnsCommittedCursorWhenUnknownOutcomeResolvesAsCommitted()
    {
        Mock<IAzureBrookRepository> repository = new(MockBehavior.Strict);
        Mock<IDistributedLockManager> lockManager = new(MockBehavior.Strict);
        Mock<IBrookRecoveryService> recoveryService = new(MockBehavior.Strict);
        Mock<IDistributedLock> distributedLock = new(MockBehavior.Strict);
        BrookKey brookId = new("orders", "123");

        distributedLock.SetupGet(item => item.LockId).Returns("lease-1");
        distributedLock.Setup(item => item.DisposeAsync()).Returns(default(ValueTask));
        distributedLock.Setup(item => item.RenewAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        lockManager.Setup(manager => manager.AcquireLockAsync(
                brookId,
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(distributedLock.Object);
        recoveryService.SetupSequence(service => service.GetOrRecoverCursorPositionAsync(
                brookId,
                distributedLock.Object,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrookPosition(1))
            .ReturnsAsync(new BrookPosition(2));
        repository.Setup(repo => repo.TryCreatePendingWriteAsync(
                brookId,
                It.IsAny<AzureBrookPendingWriteState>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repository.Setup(repo => repo.WriteEventAsync(brookId, 2, It.IsAny<BrookEvent>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        repository.Setup(repo => repo.GetCommittedCursorAsync(brookId, It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("unknown outcome"));

        EventBrookWriter writer = new(
            repository.Object,
            lockManager.Object,
            recoveryService.Object,
            Options.Create(new BrookStorageOptions()));

        BrookPosition result = await writer.AppendEventsAsync(
            brookId,
            [CreateEvent("e-1")],
            new BrookPosition(1));

        Assert.Equal(2, result.Value);
    }

    /// <summary>
    ///     Append rethrows the original failure when recovery proves the batch rolled back.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task AppendEventsAsyncRethrowsOriginalFailureWhenRecoveryResolvesAsRolledBack()
    {
        Mock<IAzureBrookRepository> repository = new(MockBehavior.Strict);
        Mock<IDistributedLockManager> lockManager = new(MockBehavior.Strict);
        Mock<IBrookRecoveryService> recoveryService = new(MockBehavior.Strict);
        Mock<IDistributedLock> distributedLock = new(MockBehavior.Strict);
        BrookKey brookId = new("orders", "123");

        distributedLock.SetupGet(item => item.LockId).Returns("lease-1");
        distributedLock.Setup(item => item.DisposeAsync()).Returns(default(ValueTask));
        distributedLock.Setup(item => item.RenewAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        lockManager.Setup(manager => manager.AcquireLockAsync(
                brookId,
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(distributedLock.Object);
        recoveryService.SetupSequence(service => service.GetOrRecoverCursorPositionAsync(
                brookId,
                distributedLock.Object,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrookPosition(1))
            .ReturnsAsync(new BrookPosition(1));
        repository.Setup(repo => repo.TryCreatePendingWriteAsync(
                brookId,
                It.IsAny<AzureBrookPendingWriteState>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repository.Setup(repo => repo.WriteEventAsync(brookId, 2, It.IsAny<BrookEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("upload failed"));

        EventBrookWriter writer = new(
            repository.Object,
            lockManager.Object,
            recoveryService.Object,
            Options.Create(new BrookStorageOptions()));

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => writer.AppendEventsAsync(
            brookId,
            [CreateEvent("e-1")],
            new BrookPosition(1)));

        Assert.Equal("upload failed", exception.Message);
    }

    /// <summary>
    ///     Append reports an ambiguous outcome when an unknown failure leaves reconciliation between the original and target cursors.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task AppendEventsAsyncThrowsAmbiguousOutcomeExceptionWhenUnknownFailureResolvesToIntermediateCursor()
    {
        Mock<IAzureBrookRepository> repository = new(MockBehavior.Strict);
        Mock<IDistributedLockManager> lockManager = new(MockBehavior.Strict);
        Mock<IBrookRecoveryService> recoveryService = new(MockBehavior.Strict);
        Mock<IDistributedLock> distributedLock = new(MockBehavior.Strict);
        BrookKey brookId = new("orders", "123");

        distributedLock.SetupGet(item => item.LockId).Returns("lease-1");
        distributedLock.Setup(item => item.DisposeAsync()).Returns(default(ValueTask));
        distributedLock.Setup(item => item.RenewAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        lockManager.Setup(manager => manager.AcquireLockAsync(
                brookId,
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(distributedLock.Object);
        recoveryService.SetupSequence(service => service.GetOrRecoverCursorPositionAsync(
                brookId,
                distributedLock.Object,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrookPosition(1))
            .ReturnsAsync(new BrookPosition(2));
        repository.Setup(repo => repo.TryCreatePendingWriteAsync(
                brookId,
                It.IsAny<AzureBrookPendingWriteState>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repository.Setup(repo => repo.WriteEventAsync(brookId, 2, It.IsAny<BrookEvent>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        repository.Setup(repo => repo.WriteEventAsync(brookId, 3, It.IsAny<BrookEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("upload failed"));

        EventBrookWriter writer = new(
            repository.Object,
            lockManager.Object,
            recoveryService.Object,
            Options.Create(new BrookStorageOptions()));

        BrookStorageAmbiguousOutcomeException exception = await Assert.ThrowsAsync<BrookStorageAmbiguousOutcomeException>(() => writer.AppendEventsAsync(
            brookId,
            [CreateEvent("e-1"), CreateEvent("e-2")],
            new BrookPosition(1)));

        Assert.IsType<InvalidOperationException>(exception.InnerException);
        Assert.Equal("upload failed", exception.InnerException?.Message);
    }

    private static BrookEvent CreateEvent(
        string eventId
    ) =>
        new()
        {
            Id = eventId,
            Source = "orders",
            EventType = "created",
            Data = ImmutableArray<byte>.Empty,
        };
}