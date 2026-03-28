using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Runtime.Storage.Azure.Brooks;
using Mississippi.Brooks.Runtime.Storage.Azure.Locking;
using Mississippi.Brooks.Runtime.Storage.Azure.Storage;

using Moq;


namespace Mississippi.Brooks.Runtime.Storage.Azure.L0Tests;

/// <summary>
///     Tests for <see cref="BrookRecoveryService" />.
/// </summary>
public sealed class BrookRecoveryServiceTests
{
    /// <summary>
    ///     Recovery commits a pending range when every expected event blob exists.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task GetOrRecoverCursorPositionAsyncCommitsPendingWhenAllEventsExist()
    {
        Mock<IAzureBrookRepository> repository = new(MockBehavior.Strict);
        Mock<IDistributedLockManager> lockManager = new(MockBehavior.Strict);
        Mock<IDistributedLock> distributedLock = new(MockBehavior.Strict);
        FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 3, 28, 0, 5, 0, TimeSpan.Zero));
        BrookKey brookId = new("orders", "123");
        AzureBrookCommittedCursorState cursor = new()
        {
            Position = 0,
        };
        AzureBrookPendingWriteState pendingWrite = new()
        {
            AttemptId = "attempt",
            LeaseId = "expired-lease",
            OriginalPosition = 0,
            TargetPosition = 2,
            CreatedUtc = timeProvider.GetUtcNow().AddMinutes(-2),
        };

        distributedLock.SetupGet(item => item.LockId).Returns("new-lease");
        repository.Setup(repo => repo.GetCommittedCursorAsync(brookId, It.IsAny<CancellationToken>())).ReturnsAsync(cursor);
        repository.Setup(repo => repo.GetPendingWriteAsync(brookId, It.IsAny<CancellationToken>())).ReturnsAsync(pendingWrite);
        repository.Setup(repo => repo.EventExistsAsync(brookId, 1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repository.Setup(repo => repo.EventExistsAsync(brookId, 2, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repository.Setup(repo => repo.TryAdvanceCommittedCursorAsync(brookId, cursor, pendingWrite, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repository.Setup(repo => repo.TryDeletePendingWriteAsync(brookId, pendingWrite, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        BrookRecoveryService service = new(
            repository.Object,
            lockManager.Object,
            Options.Create(new BrookStorageOptions()),
            timeProvider);

        BrookPosition result = await service.GetOrRecoverCursorPositionAsync(brookId, distributedLock.Object);

        Assert.Equal(2, result.Value);
        lockManager.VerifyNoOtherCalls();
        repository.VerifyAll();
    }

    /// <summary>
    ///     Recovery can safely commit stale pending state after ownership changes and the prior lease expires.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task GetOrRecoverCursorPositionAsyncAcquiresNewLockAndCommitsExpiredPendingAfterOwnershipChange()
    {
        Mock<IAzureBrookRepository> repository = new(MockBehavior.Strict);
        Mock<IDistributedLockManager> lockManager = new(MockBehavior.Strict);
        Mock<IDistributedLock> distributedLock = new(MockBehavior.Strict);
        FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 3, 28, 0, 5, 0, TimeSpan.Zero));
        BrookKey brookId = new("orders", "123");
        AzureBrookCommittedCursorState cursor = new()
        {
            Position = 1,
        };
        AzureBrookPendingWriteState pendingWrite = new()
        {
            AttemptId = "attempt",
            LeaseId = "expired-lease",
            OriginalPosition = 1,
            TargetPosition = 2,
            CreatedUtc = timeProvider.GetUtcNow().AddMinutes(-2),
        };

        distributedLock.SetupGet(item => item.LockId).Returns("replacement-lease");
        distributedLock.Setup(item => item.DisposeAsync()).Returns(default(ValueTask));
        repository.SetupSequence(repo => repo.GetPendingWriteAsync(brookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendingWrite)
            .ReturnsAsync(pendingWrite);
        lockManager.Setup(manager => manager.AcquireLockAsync(
                brookId,
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(distributedLock.Object);
        repository.Setup(repo => repo.GetCommittedCursorAsync(brookId, It.IsAny<CancellationToken>())).ReturnsAsync(cursor);
        repository.Setup(repo => repo.EventExistsAsync(brookId, 2, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repository.Setup(repo => repo.TryAdvanceCommittedCursorAsync(brookId, cursor, pendingWrite, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repository.Setup(repo => repo.TryDeletePendingWriteAsync(brookId, pendingWrite, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        BrookRecoveryService service = new(
            repository.Object,
            lockManager.Object,
            Options.Create(new BrookStorageOptions()),
            timeProvider);

        BrookPosition result = await service.GetOrRecoverCursorPositionAsync(brookId);

        Assert.Equal(2, result.Value);
        lockManager.VerifyAll();
        repository.VerifyAll();
    }

    /// <summary>
    ///     Recovery rolls back a pending range when any event blob is missing.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task GetOrRecoverCursorPositionAsyncRollsBackWhenAnyEventIsMissing()
    {
        Mock<IAzureBrookRepository> repository = new(MockBehavior.Strict);
        Mock<IDistributedLockManager> lockManager = new(MockBehavior.Strict);
        Mock<IDistributedLock> distributedLock = new(MockBehavior.Strict);
        FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 3, 28, 0, 5, 0, TimeSpan.Zero));
        BrookKey brookId = new("orders", "123");
        AzureBrookCommittedCursorState cursor = new()
        {
            Position = 0,
        };
        AzureBrookPendingWriteState pendingWrite = new()
        {
            AttemptId = "attempt",
            LeaseId = "expired-lease",
            OriginalPosition = 0,
            TargetPosition = 2,
            CreatedUtc = timeProvider.GetUtcNow().AddMinutes(-2),
        };

        distributedLock.SetupGet(item => item.LockId).Returns("new-lease");
        repository.Setup(repo => repo.GetCommittedCursorAsync(brookId, It.IsAny<CancellationToken>())).ReturnsAsync(cursor);
        repository.Setup(repo => repo.GetPendingWriteAsync(brookId, It.IsAny<CancellationToken>())).ReturnsAsync(pendingWrite);
        repository.Setup(repo => repo.EventExistsAsync(brookId, 1, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        repository.Setup(repo => repo.DeleteEventIfExistsAsync(brookId, 1, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        repository.Setup(repo => repo.DeleteEventIfExistsAsync(brookId, 2, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        repository.Setup(repo => repo.TryDeletePendingWriteAsync(brookId, pendingWrite, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        BrookRecoveryService service = new(
            repository.Object,
            lockManager.Object,
            Options.Create(new BrookStorageOptions()),
            timeProvider);

        BrookPosition result = await service.GetOrRecoverCursorPositionAsync(brookId, distributedLock.Object);

        Assert.Equal(0, result.Value);
        lockManager.VerifyNoOtherCalls();
        repository.VerifyAll();
    }

    /// <summary>
    ///     Recovery removes stale pending state when the committed cursor has already advanced.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task GetOrRecoverCursorPositionAsyncCleansPendingWhenCursorAlreadyAdvanced()
    {
        Mock<IAzureBrookRepository> repository = new(MockBehavior.Strict);
        Mock<IDistributedLockManager> lockManager = new(MockBehavior.Strict);
        Mock<IDistributedLock> distributedLock = new(MockBehavior.Strict);
        BrookKey brookId = new("orders", "123");
        AzureBrookCommittedCursorState cursor = new()
        {
            Position = 5,
        };
        AzureBrookPendingWriteState pendingWrite = new()
        {
            AttemptId = "attempt",
            LeaseId = "expired-lease",
            OriginalPosition = 3,
            TargetPosition = 5,
            CreatedUtc = new DateTimeOffset(2026, 3, 28, 0, 0, 0, TimeSpan.Zero),
        };

        distributedLock.SetupGet(item => item.LockId).Returns("new-lease");
        repository.Setup(repo => repo.GetCommittedCursorAsync(brookId, It.IsAny<CancellationToken>())).ReturnsAsync(cursor);
        repository.Setup(repo => repo.GetPendingWriteAsync(brookId, It.IsAny<CancellationToken>())).ReturnsAsync(pendingWrite);
        repository.Setup(repo => repo.TryDeletePendingWriteAsync(brookId, pendingWrite, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        BrookRecoveryService service = new(
            repository.Object,
            lockManager.Object,
            Options.Create(new BrookStorageOptions()));

        BrookPosition result = await service.GetOrRecoverCursorPositionAsync(brookId, distributedLock.Object);

        Assert.Equal(5, result.Value);
        repository.VerifyAll();
        lockManager.VerifyNoOtherCalls();
    }

    /// <summary>
    ///     Recovery stays retryable while a pending write may still belong to a live writer.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task GetOrRecoverCursorPositionAsyncThrowsRetryableExceptionWhenPendingWriteMayStillBeLive()
    {
        Mock<IAzureBrookRepository> repository = new(MockBehavior.Strict);
        Mock<IDistributedLockManager> lockManager = new(MockBehavior.Strict);
        Mock<IDistributedLock> distributedLock = new(MockBehavior.Strict);
        FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 3, 28, 0, 0, 30, TimeSpan.Zero));
        BrookKey brookId = new("orders", "123");
        AzureBrookPendingWriteState pendingWrite = new()
        {
            AttemptId = "attempt",
            LeaseId = "active-lease",
            OriginalPosition = 0,
            TargetPosition = 1,
            CreatedUtc = timeProvider.GetUtcNow().AddSeconds(-10),
        };

        distributedLock.SetupGet(item => item.LockId).Returns("different-lease");
        repository.Setup(repo => repo.GetCommittedCursorAsync(brookId, It.IsAny<CancellationToken>())).ReturnsAsync((AzureBrookCommittedCursorState?)null);
        repository.Setup(repo => repo.GetPendingWriteAsync(brookId, It.IsAny<CancellationToken>())).ReturnsAsync(pendingWrite);

        BrookRecoveryService service = new(
            repository.Object,
            lockManager.Object,
            Options.Create(new BrookStorageOptions()),
            timeProvider);

        BrookStorageRetryableException exception = await Assert.ThrowsAsync<BrookStorageRetryableException>(() => service.GetOrRecoverCursorPositionAsync(
            brookId,
            distributedLock.Object));

        Assert.Contains(brookId.ToString(), exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Recovery keeps readers retryable when they encounter a live writer through the public lock-acquiring path.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task GetOrRecoverCursorPositionAsyncAcquiresLockAndThrowsRetryableExceptionWhenPendingWriteMayStillBeLive()
    {
        Mock<IAzureBrookRepository> repository = new(MockBehavior.Strict);
        Mock<IDistributedLockManager> lockManager = new(MockBehavior.Strict);
        Mock<IDistributedLock> distributedLock = new(MockBehavior.Strict);
        FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 3, 28, 0, 0, 30, TimeSpan.Zero));
        BrookKey brookId = new("orders", "123");
        AzureBrookPendingWriteState pendingWrite = new()
        {
            AttemptId = "attempt",
            LeaseId = "active-lease",
            OriginalPosition = 0,
            TargetPosition = 1,
            CreatedUtc = timeProvider.GetUtcNow().AddSeconds(-10),
        };

        distributedLock.SetupGet(item => item.LockId).Returns("different-lease");
        distributedLock.Setup(item => item.DisposeAsync()).Returns(default(ValueTask));
        repository.SetupSequence(repo => repo.GetPendingWriteAsync(brookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendingWrite)
            .ReturnsAsync(pendingWrite);
        lockManager.Setup(manager => manager.AcquireLockAsync(
                brookId,
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(distributedLock.Object);
        repository.Setup(repo => repo.GetCommittedCursorAsync(brookId, It.IsAny<CancellationToken>())).ReturnsAsync((AzureBrookCommittedCursorState?)null);

        BrookRecoveryService service = new(
            repository.Object,
            lockManager.Object,
            Options.Create(new BrookStorageOptions()),
            timeProvider);

        BrookStorageRetryableException exception = await Assert.ThrowsAsync<BrookStorageRetryableException>(() => service.GetOrRecoverCursorPositionAsync(brookId));

        Assert.Contains(brookId.ToString(), exception.Message, StringComparison.Ordinal);
        lockManager.VerifyAll();
        repository.VerifyAll();
    }

    /// <summary>
    ///     Recovery reports an ambiguous outcome when commit state cannot be resolved safely.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task GetOrRecoverCursorPositionAsyncThrowsAmbiguousOutcomeExceptionWhenCommitStateCannotBeResolved()
    {
        Mock<IAzureBrookRepository> repository = new(MockBehavior.Strict);
        Mock<IDistributedLockManager> lockManager = new(MockBehavior.Strict);
        Mock<IDistributedLock> distributedLock = new(MockBehavior.Strict);
        FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 3, 28, 0, 5, 0, TimeSpan.Zero));
        BrookKey brookId = new("orders", "123");
        AzureBrookCommittedCursorState cursor = new()
        {
            Position = 0,
        };
        AzureBrookPendingWriteState pendingWrite = new()
        {
            AttemptId = "attempt",
            LeaseId = "expired-lease",
            OriginalPosition = 0,
            TargetPosition = 2,
            CreatedUtc = timeProvider.GetUtcNow().AddMinutes(-2),
        };

        distributedLock.SetupGet(item => item.LockId).Returns("new-lease");
        repository.SetupSequence(repo => repo.GetCommittedCursorAsync(brookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cursor)
            .ReturnsAsync(cursor);
        repository.SetupSequence(repo => repo.GetPendingWriteAsync(brookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendingWrite)
            .ReturnsAsync(pendingWrite);
        repository.Setup(repo => repo.EventExistsAsync(brookId, 1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repository.Setup(repo => repo.EventExistsAsync(brookId, 2, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repository.Setup(repo => repo.TryAdvanceCommittedCursorAsync(brookId, cursor, pendingWrite, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        BrookRecoveryService service = new(
            repository.Object,
            lockManager.Object,
            Options.Create(new BrookStorageOptions()),
            timeProvider);

        await Assert.ThrowsAsync<BrookStorageAmbiguousOutcomeException>(() => service.GetOrRecoverCursorPositionAsync(
            brookId,
            distributedLock.Object));
    }
}