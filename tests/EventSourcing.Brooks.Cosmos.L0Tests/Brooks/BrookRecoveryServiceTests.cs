using System;
using System.Threading;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Azure;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Mississippi.Common.Cosmos.Abstractions.Retry;
using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Cosmos.Brooks;
using Mississippi.EventSourcing.Brooks.Cosmos.Locking;
using Mississippi.EventSourcing.Brooks.Cosmos.Storage;

using Moq;


namespace Mississippi.EventSourcing.Brooks.Cosmos.L0Tests.Brooks;

/// <summary>
///     Tests for <see cref="BrookRecoveryService" /> behavior under cursor/lock scenarios.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Brooks Cosmos")]
[AllureSubSuite("Brook Recovery Service")]
public sealed class BrookRecoveryServiceTests
{
    private sealed class TestRetryPolicy : IRetryPolicy
    {
        /// <summary>
        ///     Executes the provided operation and returns its result.
        /// </summary>
        /// <typeparam name="T">The operation result type.</typeparam>
        /// <param name="operation">The async operation to execute.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that completes with the operation result.</returns>
        public async Task<T> ExecuteAsync<T>(
            Func<Task<T>> operation,
            CancellationToken cancellationToken = default
        ) =>
            await operation().ConfigureAwait(false);
    }

    /// <summary>
    ///     Verifies constructor validates null options.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenOptionsIsNull()
    {
        ICosmosRepository repo = new Mock<ICosmosRepository>(MockBehavior.Strict).Object;
        IRetryPolicy retry = new Mock<IRetryPolicy>(MockBehavior.Strict).Object;
        IDistributedLockManager lockMgr = new Mock<IDistributedLockManager>(MockBehavior.Strict).Object;
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new BrookRecoveryService(repo, retry, lockMgr, null!, NullLogger<BrookRecoveryService>.Instance));
        Assert.Equal("options", ex.ParamName);
    }

    /// <summary>
    ///     Commits pending cursor state when all missing events exist.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation which completes when the assertion has run.</returns>
    [Fact]
    public async Task GetOrRecoverCursorPositionAsyncCommitsPendingOnAllEventsExistAsync()
    {
        Mock<ICosmosRepository> repo = new(MockBehavior.Strict);
        Mock<IDistributedLockManager> lockMgr = new(MockBehavior.Strict);
        BrookKey brookId = new("t", "i");

        // Sequence for GetCursorDocumentAsync: null (first), then after commit returns the cursor document with target position
        repo.SetupSequence(r => r.GetCursorDocumentAsync(brookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CursorStorageModel?)null)
            .ReturnsAsync(
                new CursorStorageModel
                {
                    Position = new(3),
                });

        // pending cursor indicates original 1 -> target 3 (positions 2 and 3 must exist)
        CursorStorageModel pending = new()
        {
            OriginalPosition = new BrookPosition(1),
            Position = new(3),
        };
        repo.Setup(r => r.GetPendingCursorDocumentAsync(brookId, It.IsAny<CancellationToken>())).ReturnsAsync(pending);

        // EventExists should return true for positions 2 and 3
        repo.Setup(r => r.EventExistsAsync(brookId, 2, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repo.Setup(r => r.EventExistsAsync(brookId, 3, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repo.Setup(r => r.CommitCursorPositionAsync(brookId, 3, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        Mock<IDistributedLock> lockMock1 = new(MockBehavior.Strict);
        lockMock1.Setup(l => l.DisposeAsync()).Returns(default(ValueTask));
        lockMgr.Setup(m => m.AcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lockMock1.Object);
        BrookRecoveryService service = new(
            repo.Object,
            new TestRetryPolicy(),
            lockMgr.Object,
            Options.Create(
                new BrookStorageOptions
                {
                    LeaseDurationSeconds = 5,
                }),
            NullLogger<BrookRecoveryService>.Instance);
        BrookPosition result = await service.GetOrRecoverCursorPositionAsync(brookId);
        Assert.Equal(3, result.Value);
        repo.Verify(r => r.CommitCursorPositionAsync(brookId, 3, It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.DeletePendingCursorAsync(It.IsAny<BrookKey>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    ///     Returns -1 when no cursor and no pending cursor data is available.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation which completes when the assertion has run.</returns>
    [Fact]
    public async Task GetOrRecoverCursorPositionAsyncReturnsMinusOneWhenNoCursorAsync()
    {
        Mock<ICosmosRepository> repo = new(MockBehavior.Strict);
        Mock<IDistributedLockManager> lockMgr = new(MockBehavior.Strict);

        // No cursor document and no pending cursor entry
        repo.Setup(r => r.GetCursorDocumentAsync(It.IsAny<BrookKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CursorStorageModel?)null);
        repo.Setup(r => r.GetPendingCursorDocumentAsync(It.IsAny<BrookKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CursorStorageModel?)null);
        BrookRecoveryService service = new(
            repo.Object,
            new TestRetryPolicy(),
            lockMgr.Object,
            Options.Create(new BrookStorageOptions()),
            NullLogger<BrookRecoveryService>.Instance);
        BrookPosition result = await service.GetOrRecoverCursorPositionAsync(new("t", "i"));
        Assert.Equal(-1, result.Value);
        repo.Verify(
            r => r.GetCursorDocumentAsync(It.IsAny<BrookKey>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
        repo.Verify(
            r => r.GetPendingCursorDocumentAsync(It.IsAny<BrookKey>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    ///     Rolls back when events referenced by the pending cursor entry are missing.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation which completes when the assertion has run.</returns>
    [Fact]
    public async Task GetOrRecoverCursorPositionAsyncRollsBackWhenEventsMissingAsync()
    {
        Mock<ICosmosRepository> repo = new(MockBehavior.Strict);
        Mock<IDistributedLockManager> lockMgr = new(MockBehavior.Strict);
        BrookKey brookId = new("t", "i2");
        repo.SetupSequence(r => r.GetCursorDocumentAsync(brookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CursorStorageModel?)null)
            .ReturnsAsync((CursorStorageModel?)null);
        CursorStorageModel pending = new()
        {
            OriginalPosition = new BrookPosition(0),
            Position = new(2),
        };
        repo.Setup(r => r.GetPendingCursorDocumentAsync(brookId, It.IsAny<CancellationToken>())).ReturnsAsync(pending);

        // Simulate missing event at position 1 (first to check) -> triggers rollback
        repo.Setup(r => r.EventExistsAsync(brookId, 1, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Expect DeleteEventAsync for positions 1..2 and DeletePendingCursorAsync once
        repo.Setup(r => r.DeleteEventAsync(brookId, 1, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        repo.Setup(r => r.DeleteEventAsync(brookId, 2, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        repo.Setup(r => r.DeletePendingCursorAsync(brookId, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        Mock<IDistributedLock> lockMock2 = new(MockBehavior.Strict);
        lockMock2.Setup(l => l.DisposeAsync()).Returns(default(ValueTask));
        lockMgr.Setup(m => m.AcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lockMock2.Object);
        BrookRecoveryService service = new(
            repo.Object,
            new TestRetryPolicy(),
            lockMgr.Object,
            Options.Create(
                new BrookStorageOptions
                {
                    LeaseDurationSeconds = 5,
                }),
            NullLogger<BrookRecoveryService>.Instance);
        BrookPosition result = await service.GetOrRecoverCursorPositionAsync(brookId);

        // After rollback there is no cursor document, so expect -1
        Assert.Equal(-1, result.Value);
        repo.Verify(r => r.DeleteEventAsync(brookId, 1, It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.DeleteEventAsync(brookId, 2, It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.DeletePendingCursorAsync(brookId, It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(
            r => r.CommitCursorPositionAsync(It.IsAny<BrookKey>(), It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    ///     Throws when the cursor remains unresolved after waiting.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation which completes when the assertion has run.</returns>
    [Fact]
    public async Task GetOrRecoverCursorPositionAsyncThrowsWhenCursorStillNullAfterWaitAsync()
    {
        Mock<ICosmosRepository> repo = new(MockBehavior.Strict);
        Mock<IDistributedLockManager> lockMgr = new(MockBehavior.Strict);
        BrookKey brookId = new("t", "i4");
        repo.SetupSequence(r => r.GetCursorDocumentAsync(brookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CursorStorageModel?)null)
            .ReturnsAsync((CursorStorageModel?)null);
        repo.Setup(r => r.GetPendingCursorDocumentAsync(brookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new CursorStorageModel
                {
                    OriginalPosition = new BrookPosition(0),
                    Position = new(1),
                });
        lockMgr.Setup(m => m.AcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException("conflict"));
        BrookRecoveryService service = new(
            repo.Object,
            new TestRetryPolicy(),
            lockMgr.Object,
            Options.Create(
                new BrookStorageOptions
                {
                    LeaseDurationSeconds = 1,
                }),
            NullLogger<BrookRecoveryService>.Instance);
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await service.GetOrRecoverCursorPositionAsync(brookId));
    }

    /// <summary>
    ///     Waits and reads cursor when recovery lock cannot be acquired.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation which completes when the assertion has run.</returns>
    [Fact]
    public async Task GetOrRecoverCursorPositionAsyncWaitsWhenRecoveryLockUnavailableAsync()
    {
        Mock<ICosmosRepository> repo = new(MockBehavior.Strict);
        Mock<IDistributedLockManager> lockMgr = new(MockBehavior.Strict);
        BrookKey brookId = new("t", "i3");

        // First read returns null, second read (after wait) returns a cursor document
        repo.SetupSequence(r => r.GetCursorDocumentAsync(brookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CursorStorageModel?)null)
            .ReturnsAsync(
                new CursorStorageModel
                {
                    Position = new(7),
                });
        repo.Setup(r => r.GetPendingCursorDocumentAsync(brookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new CursorStorageModel
                {
                    OriginalPosition = new BrookPosition(0),
                    Position = new(5),
                });

        // AcquireLock throws RequestFailedException to simulate contention
        lockMgr.Setup(m => m.AcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException("conflict"));
        BrookRecoveryService service = new(
            repo.Object,
            new TestRetryPolicy(),
            lockMgr.Object,
            Options.Create(
                new BrookStorageOptions
                {
                    LeaseDurationSeconds = 1,
                }),
            NullLogger<BrookRecoveryService>.Instance);
        BrookPosition result = await service.GetOrRecoverCursorPositionAsync(brookId);
        Assert.Equal(7, result.Value);
    }
}