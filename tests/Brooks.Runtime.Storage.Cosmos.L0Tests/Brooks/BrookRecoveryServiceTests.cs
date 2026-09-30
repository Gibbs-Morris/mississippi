using System;
using System.Threading;
using System.Threading.Tasks;

using Azure;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Brooks;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Locking;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Storage;
using Mississippi.Common.Runtime.Storage.Abstractions.Retry;

using Moq;


namespace Mississippi.Brooks.Runtime.Storage.Cosmos.L0Tests.Brooks;

/// <summary>
///     Tests for <see cref="BrookRecoveryService" /> behavior under cursor/lock scenarios.
/// </summary>
public sealed class BrookRecoveryServiceTests
{
    private sealed class TestRetryPolicy : IRetryPolicy
    {
        public TestRetryPolicy(
            bool retryOnce = false
        ) =>
            RetryOnce = retryOnce;

        private bool RetryOnce { get; }

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
        )
        {
            try
            {
                return await operation();
            }
            catch (InvalidOperationException) when (RetryOnce)
            {
                return await operation();
            }
        }
    }

    /// <summary>
    ///     Checks the pending event range before clearing metadata left by a committed append.
    /// </summary>
    /// <param name="lastEventExists">Whether the last event is visible.</param>
    /// <param name="transientCleanupFailure">Whether the first conditional cleanup attempt fails.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task CommittedCursorClearsPendingOnlyAfterRangeVerificationAsync(
        bool lastEventExists,
        bool transientCleanupFailure
    )
    {
        BrookKey key = new("test", "committed-pending");
        Mock<ICosmosRepository> repository = new(MockBehavior.Strict);
        CursorStorageModel pending = new()
        {
            ETag = "old-attempt",
            OriginalPosition = new(1),
            Position = new(3),
        };
        repository.Setup(r => r.GetPendingCursorDocumentAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pending);
        repository.Setup(r => r.GetCursorDocumentAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new CursorStorageModel
                {
                    Position = new(3),
                });
        repository.Setup(r => r.EventExistsAsync(key, 2, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repository.Setup(r => r.EventExistsAsync(key, 3, It.IsAny<CancellationToken>())).ReturnsAsync(lastEventExists);
        if (transientCleanupFailure)
        {
            repository.SetupSequence(r => r.DeletePendingCursorAsync(key, "old-attempt", It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("transient cleanup failure"))
                .Returns(Task.CompletedTask);
        }
        else
        {
            repository.Setup(r => r.DeletePendingCursorAsync(key, "old-attempt", It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        Mock<IDistributedLock> writerLock = new(MockBehavior.Strict);
        writerLock.Setup(l => l.RenewAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        writerLock.Setup(l => l.DisposeAsync()).Returns(default(ValueTask));
        Mock<IDistributedLockManager> lockManager = new(MockBehavior.Strict);
        lockManager.Setup(l => l.AcquireLockAsync(key.ToString(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(writerLock.Object);
        BrookRecoveryService service = new(
            repository.Object,
            new TestRetryPolicy(transientCleanupFailure),
            lockManager.Object,
            Options.Create(new BrookStorageOptions()),
            NullLogger<BrookRecoveryService>.Instance);
        if (lastEventExists)
        {
            BrookPosition position = await service.GetOrRecoverCursorPositionAsync(
                key,
                TestContext.Current.CancellationToken);
            Assert.Equal(3, position.Value);
        }
        else
        {
            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.GetOrRecoverCursorPositionAsync(key, TestContext.Current.CancellationToken));
            Assert.Contains("outcome remains unresolved", exception.Message, StringComparison.Ordinal);
        }

        Times expectedDeletes = Times.Never();
        if (lastEventExists)
        {
            expectedDeletes = Times.Exactly(transientCleanupFailure ? 2 : 1);
        }

        repository.Verify(
            r => r.DeletePendingCursorAsync(key, "old-attempt", It.IsAny<CancellationToken>()),
            expectedDeletes);
        if (transientCleanupFailure)
        {
            writerLock.Verify(l => l.RenewAsync(It.IsAny<CancellationToken>()), Times.AtLeast(4));
        }

        repository.Verify(
            r => r.CommitCursorPositionAsync(
                It.IsAny<BrookKey>(),
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
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
    ///     Does not substitute a cursor read when the shared writer lock cannot be acquired.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation which completes when the assertion has run.</returns>
    [Fact]
    public async Task GetOrRecoverCursorPositionAsyncDoesNotFallBackWhenWriterLockUnavailableAsync()
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
        await Assert.ThrowsAsync<RequestFailedException>(() => service.GetOrRecoverCursorPositionAsync(
            brookId,
            TestContext.Current.CancellationToken));
        repo.Verify(r => r.GetPendingCursorDocumentAsync(brookId, It.IsAny<CancellationToken>()), Times.Once);
        repo.VerifyNoOtherCalls();
    }

    /// <summary>
    ///     Reads missing and existing streams without allocating a lease when no pending append is visible.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation which completes when the assertion has run.</returns>
    /// <param name="position">The committed cursor position, or the unset sentinel.</param>
    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public async Task GetOrRecoverCursorPositionAsyncReadsWithoutLeaseWhenNoPendingAsync(
        long position
    )
    {
        Mock<ICosmosRepository> repo = new(MockBehavior.Strict);
        Mock<IDistributedLockManager> lockMgr = new(MockBehavior.Strict);
        repo.Setup(r => r.GetCursorDocumentAsync(It.IsAny<BrookKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                position < 0
                    ? null
                    : new CursorStorageModel
                    {
                        Position = new(position),
                    });
        repo.Setup(r => r.GetPendingCursorDocumentAsync(It.IsAny<BrookKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CursorStorageModel?)null);
        BrookRecoveryService service = new(
            repo.Object,
            new TestRetryPolicy(),
            lockMgr.Object,
            Options.Create(new BrookStorageOptions()),
            NullLogger<BrookRecoveryService>.Instance);
        BrookPosition result = await service.GetOrRecoverCursorPositionAsync(
            new("t", "i"),
            TestContext.Current.CancellationToken);
        Assert.Equal(position, result.Value);
        lockMgr.VerifyNoOtherCalls();
        repo.Verify(
            r => r.GetCursorDocumentAsync(It.IsAny<BrookKey>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
        repo.Verify(
            r => r.GetPendingCursorDocumentAsync(It.IsAny<BrookKey>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    ///     Retains incomplete pending history until a delayed write becomes visible.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation which completes when the assertion has run.</returns>
    [Fact]
    public async Task GetOrRecoverCursorPositionAsyncRetainsPendingHistoryUntilLateWriteArrivesAsync()
    {
        Mock<ICosmosRepository> repo = new(MockBehavior.Strict);
        Mock<IDistributedLockManager> lockMgr = new(MockBehavior.Strict);
        BrookKey brookId = new("t", "i2");
        repo.SetupSequence(r => r.GetCursorDocumentAsync(brookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new CursorStorageModel
                {
                    Position = new(0),
                })
            .ReturnsAsync(
                new CursorStorageModel
                {
                    Position = new(0),
                });
        CursorStorageModel pending = new()
        {
            ETag = "attempt-etag",
            OriginalPosition = new BrookPosition(0),
            Position = new(2),
        };
        repo.Setup(r => r.GetPendingCursorDocumentAsync(brookId, It.IsAny<CancellationToken>())).ReturnsAsync(pending);
        repo.SetupSequence(r => r.EventExistsAsync(brookId, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false)
            .ReturnsAsync(true);
        repo.Setup(r => r.EventExistsAsync(brookId, 2, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repo.Setup(r => r.CommitCursorPositionAsync(brookId, 2, "attempt-etag", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        Mock<IDistributedLock> lockMock2 = new(MockBehavior.Strict);
        lockMock2.Setup(l => l.RenewAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        lockMock2.Setup(l => l.DisposeAsync()).Returns(default(ValueTask));
        lockMgr.Setup(m => m.AcquireLockAsync(brookId.ToString(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
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
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetOrRecoverCursorPositionAsync(brookId, TestContext.Current.CancellationToken));
        Assert.Contains("outcome remains unresolved", exception.Message, StringComparison.Ordinal);
        repo.Verify(
            r => r.CommitCursorPositionAsync(
                It.IsAny<BrookKey>(),
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        BrookPosition result = await service.GetOrRecoverCursorPositionAsync(
            brookId,
            TestContext.Current.CancellationToken);
        Assert.Equal(2, result.Value);
        repo.Verify(
            r => r.CommitCursorPositionAsync(brookId, 2, "attempt-etag", It.IsAny<CancellationToken>()),
            Times.Once);
        repo.Verify(r => r.DeleteEventAsync(brookId, It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
        repo.Verify(
            r => r.DeletePendingCursorAsync(brookId, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    ///     Returns the committed target even when cursor reads still expose the earlier position.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation which completes when the assertion has run.</returns>
    [Fact]
    public async Task GetOrRecoverCursorPositionAsyncReturnsCommittedTargetWhenCursorReadRemainsStaleAsync()
    {
        Mock<ICosmosRepository> repo = new(MockBehavior.Strict);
        Mock<IDistributedLockManager> lockMgr = new(MockBehavior.Strict);
        BrookKey brookId = new("t", "i");

        // Earlier committed history must not hide a complete pending operation.
        repo.Setup(r => r.GetCursorDocumentAsync(brookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new CursorStorageModel
                {
                    Position = new(1),
                });

        // pending cursor indicates original 1 -> target 3 (positions 2 and 3 must exist)
        CursorStorageModel pending = new()
        {
            ETag = "attempt-etag",
            OriginalPosition = new BrookPosition(1),
            Position = new(3),
        };
        repo.Setup(r => r.GetPendingCursorDocumentAsync(brookId, It.IsAny<CancellationToken>())).ReturnsAsync(pending);

        // EventExists should return true for positions 2 and 3
        repo.Setup(r => r.EventExistsAsync(brookId, 2, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repo.Setup(r => r.EventExistsAsync(brookId, 3, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repo.Setup(r => r.CommitCursorPositionAsync(brookId, 3, "attempt-etag", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        Mock<IDistributedLock> lockMock1 = new(MockBehavior.Strict);
        lockMock1.Setup(l => l.RenewAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        lockMock1.Setup(l => l.DisposeAsync()).Returns(default(ValueTask));
        lockMgr.Setup(m => m.AcquireLockAsync(brookId.ToString(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
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
        BrookPosition result = await service.GetOrRecoverCursorPositionAsync(
            brookId,
            TestContext.Current.CancellationToken);
        Assert.Equal(3, result.Value);
        repo.Verify(r => r.GetCursorDocumentAsync(brookId, It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(
            r => r.CommitCursorPositionAsync(brookId, 3, "attempt-etag", It.IsAny<CancellationToken>()),
            Times.Once);
        repo.Verify(
            r => r.DeletePendingCursorAsync(It.IsAny<BrookKey>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    ///     Propagates lock unavailability without reading or modifying unconfirmed storage.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation which completes when the assertion has run.</returns>
    [Fact]
    public async Task GetOrRecoverCursorPositionAsyncThrowsWhenWriterLockUnavailableAsync()
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
        await Assert.ThrowsAsync<RequestFailedException>(async () =>
            await service.GetOrRecoverCursorPositionAsync(brookId, TestContext.Current.CancellationToken));
        repo.Verify(r => r.GetPendingCursorDocumentAsync(brookId, It.IsAny<CancellationToken>()), Times.Once);
        repo.VerifyNoOtherCalls();
    }

    /// <summary>
    ///     Uses the cursor and pending state reread after waiting for the writer lease.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ReaderRechecksPendingAfterAcquiringWriterLeaseAsync()
    {
        BrookKey key = new("test", "pending-cleared-while-waiting");
        Mock<ICosmosRepository> repository = new(MockBehavior.Strict);
        repository.SetupSequence(r => r.GetPendingCursorDocumentAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new CursorStorageModel
                {
                    ETag = "old-attempt",
                    OriginalPosition = new(0),
                    Position = new(1),
                })
            .ReturnsAsync((CursorStorageModel?)null);
        repository.Setup(r => r.GetCursorDocumentAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new CursorStorageModel
                {
                    Position = new(1),
                });
        Mock<IDistributedLock> writerLock = new(MockBehavior.Strict);
        writerLock.Setup(l => l.RenewAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        writerLock.Setup(l => l.DisposeAsync()).Returns(default(ValueTask));
        Mock<IDistributedLockManager> lockManager = new(MockBehavior.Strict);
        lockManager.Setup(l => l.AcquireLockAsync(key.ToString(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(writerLock.Object);
        BrookRecoveryService service = new(
            repository.Object,
            new TestRetryPolicy(),
            lockManager.Object,
            Options.Create(new BrookStorageOptions()),
            NullLogger<BrookRecoveryService>.Instance);
        BrookPosition position = await service.GetOrRecoverCursorPositionAsync(
            key,
            TestContext.Current.CancellationToken);
        Assert.Equal(1, position.Value);
        repository.Verify(r => r.GetPendingCursorDocumentAsync(key, It.IsAny<CancellationToken>()), Times.Exactly(2));
        repository.Verify(r => r.GetCursorDocumentAsync(key, It.IsAny<CancellationToken>()), Times.Once);
        repository.VerifyNoOtherCalls();
    }

    /// <summary>
    ///     Logs a recovery failure once when the caller already owns the writer lease.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task WriterOwnedRecoveryLogsFailureAsync()
    {
        BrookKey key = new("test", "writer-failure");
        InvalidOperationException failure = new("cursor unavailable");
        Mock<ICosmosRepository> repository = new(MockBehavior.Strict);
        repository.Setup(r => r.GetCursorDocumentAsync(key, It.IsAny<CancellationToken>())).ThrowsAsync(failure);
        Mock<IDistributedLock> writerLock = new(MockBehavior.Strict);
        writerLock.Setup(l => l.RenewAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        Mock<ILogger<BrookRecoveryService>> logger = new();
        logger.Setup(l => l.IsEnabled(LogLevel.Error)).Returns(true);
        BrookRecoveryService service = new(
            repository.Object,
            new TestRetryPolicy(),
            new Mock<IDistributedLockManager>(MockBehavior.Strict).Object,
            Options.Create(new BrookStorageOptions()),
            logger.Object);
        InvalidOperationException thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetOrRecoverCursorPositionAsync(key, writerLock.Object, TestContext.Current.CancellationToken));
        Assert.Same(failure, thrown);
        logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.Is<EventId>(id => id.Id == 11),
                It.IsAny<It.IsAnyType>(),
                failure,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}