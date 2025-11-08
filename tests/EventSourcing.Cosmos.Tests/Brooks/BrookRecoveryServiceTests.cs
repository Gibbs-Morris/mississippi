using Azure;

using Microsoft.Extensions.Options;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Cosmos.Abstractions;
using Mississippi.EventSourcing.Cosmos.Brooks;
using Mississippi.EventSourcing.Cosmos.Locking;
using Mississippi.EventSourcing.Cosmos.Retry;
using Mississippi.EventSourcing.Cosmos.Storage;

using Moq;


namespace Mississippi.EventSourcing.Cosmos.Tests.Brooks;

/// <summary>
///     Tests for <see cref="BrookRecoveryService" /> behavior under head/lock scenarios.
/// </summary>
public class BrookRecoveryServiceTests
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
        ArgumentNullException ex =
            Assert.Throws<ArgumentNullException>(() => new BrookRecoveryService(repo, retry, lockMgr, null!));
        Assert.Equal("options", ex.ParamName);
    }

    /// <summary>
    ///     Commits pending head when all missing events exist.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation which completes when the assertion has run.</returns>
    [Fact]
    public async Task GetOrRecoverHeadPositionAsyncCommitsPendingOnAllEventsExistAsync()
    {
        Mock<ICosmosRepository> repo = new(MockBehavior.Strict);
        Mock<IDistributedLockManager> lockMgr = new(MockBehavior.Strict);
        BrookKey brookId = new("t", "i");

        // Sequence for GetHeadDocumentAsync: null (first), then after commit returns head with target position
        repo.SetupSequence(r => r.GetHeadDocumentAsync(brookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HeadStorageModel?)null)
            .ReturnsAsync(
                new HeadStorageModel
                {
                    Position = new(3),
                });

        // pending head indicates original 1 -> target 3 (positions 2 and 3 must exist)
        HeadStorageModel pending = new()
        {
            OriginalPosition = new BrookPosition(1),
            Position = new(3),
        };
        repo.Setup(r => r.GetPendingHeadDocumentAsync(brookId, It.IsAny<CancellationToken>())).ReturnsAsync(pending);

        // EventExists should return true for positions 2 and 3
        repo.Setup(r => r.EventExistsAsync(brookId, 2, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repo.Setup(r => r.EventExistsAsync(brookId, 3, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repo.Setup(r => r.CommitHeadPositionAsync(brookId, 3, It.IsAny<CancellationToken>()))
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
                }));
        BrookPosition result = await service.GetOrRecoverHeadPositionAsync(brookId);
        Assert.Equal(3, result.Value);
        repo.Verify(r => r.CommitHeadPositionAsync(brookId, 3, It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.DeletePendingHeadAsync(It.IsAny<BrookKey>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    ///     Returns -1 when no head and no pending head.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation which completes when the assertion has run.</returns>
    [Fact]
    public async Task GetOrRecoverHeadPositionAsyncReturnsMinusOneWhenNoHeadAsync()
    {
        Mock<ICosmosRepository> repo = new(MockBehavior.Strict);
        Mock<IDistributedLockManager> lockMgr = new(MockBehavior.Strict);

        // No head, no pending head
        repo.Setup(r => r.GetHeadDocumentAsync(It.IsAny<BrookKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((HeadStorageModel?)null);
        repo.Setup(r => r.GetPendingHeadDocumentAsync(It.IsAny<BrookKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((HeadStorageModel?)null);
        BrookRecoveryService service = new(
            repo.Object,
            new TestRetryPolicy(),
            lockMgr.Object,
            Options.Create(new BrookStorageOptions()));
        BrookPosition result = await service.GetOrRecoverHeadPositionAsync(new("t", "i"));
        Assert.Equal(-1, result.Value);
        repo.Verify(
            r => r.GetHeadDocumentAsync(It.IsAny<BrookKey>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
        repo.Verify(
            r => r.GetPendingHeadDocumentAsync(It.IsAny<BrookKey>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    ///     Rolls back when events referenced by pending head are missing.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation which completes when the assertion has run.</returns>
    [Fact]
    public async Task GetOrRecoverHeadPositionAsyncRollsBackWhenEventsMissingAsync()
    {
        Mock<ICosmosRepository> repo = new(MockBehavior.Strict);
        Mock<IDistributedLockManager> lockMgr = new(MockBehavior.Strict);
        BrookKey brookId = new("t", "i2");
        repo.SetupSequence(r => r.GetHeadDocumentAsync(brookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HeadStorageModel?)null)
            .ReturnsAsync((HeadStorageModel?)null);
        HeadStorageModel pending = new()
        {
            OriginalPosition = new BrookPosition(0),
            Position = new(2),
        };
        repo.Setup(r => r.GetPendingHeadDocumentAsync(brookId, It.IsAny<CancellationToken>())).ReturnsAsync(pending);

        // Simulate missing event at position 1 (first to check) -> triggers rollback
        repo.Setup(r => r.EventExistsAsync(brookId, 1, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Expect DeleteEventAsync for positions 1..2 and DeletePendingHeadAsync once
        repo.Setup(r => r.DeleteEventAsync(brookId, 1, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        repo.Setup(r => r.DeleteEventAsync(brookId, 2, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        repo.Setup(r => r.DeletePendingHeadAsync(brookId, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
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
                }));
        BrookPosition result = await service.GetOrRecoverHeadPositionAsync(brookId);

        // After rollback there is no head document, so expect -1
        Assert.Equal(-1, result.Value);
        repo.Verify(r => r.DeleteEventAsync(brookId, 1, It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.DeleteEventAsync(brookId, 2, It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.DeletePendingHeadAsync(brookId, It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(
            r => r.CommitHeadPositionAsync(It.IsAny<BrookKey>(), It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    ///     Throws when head remains null after waiting.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation which completes when the assertion has run.</returns>
    [Fact]
    public async Task GetOrRecoverHeadPositionAsyncThrowsWhenHeadStillNullAfterWaitAsync()
    {
        Mock<ICosmosRepository> repo = new(MockBehavior.Strict);
        Mock<IDistributedLockManager> lockMgr = new(MockBehavior.Strict);
        BrookKey brookId = new("t", "i4");
        repo.SetupSequence(r => r.GetHeadDocumentAsync(brookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HeadStorageModel?)null)
            .ReturnsAsync((HeadStorageModel?)null);
        repo.Setup(r => r.GetPendingHeadDocumentAsync(brookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new HeadStorageModel
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
                }));
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await service.GetOrRecoverHeadPositionAsync(brookId));
    }

    /// <summary>
    ///     Waits and reads head when recovery lock cannot be acquired.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation which completes when the assertion has run.</returns>
    [Fact]
    public async Task GetOrRecoverHeadPositionAsyncWaitsWhenRecoveryLockUnavailableAsync()
    {
        Mock<ICosmosRepository> repo = new(MockBehavior.Strict);
        Mock<IDistributedLockManager> lockMgr = new(MockBehavior.Strict);
        BrookKey brookId = new("t", "i3");

        // First read returns null, second read (after wait) returns a head
        repo.SetupSequence(r => r.GetHeadDocumentAsync(brookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HeadStorageModel?)null)
            .ReturnsAsync(
                new HeadStorageModel
                {
                    Position = new(7),
                });
        repo.Setup(r => r.GetPendingHeadDocumentAsync(brookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new HeadStorageModel
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
                }));
        BrookPosition result = await service.GetOrRecoverHeadPositionAsync(brookId);
        Assert.Equal(7, result.Value);
    }
}