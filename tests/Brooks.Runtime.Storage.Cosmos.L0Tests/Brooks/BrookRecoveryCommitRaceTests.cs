using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Brooks;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Locking;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Storage;
using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Common.Runtime.Storage.Abstractions.Retry;

using Moq;


namespace Mississippi.Brooks.Runtime.Storage.Cosmos.L0Tests.Brooks;

/// <summary>
///     Verifies recovery when an older dispatched transaction commits after the recovery snapshot.
/// </summary>
public sealed class BrookRecoveryCommitRaceTests
{
    private sealed class DirectRetryPolicy : IRetryPolicy
    {
        public Task<T> ExecuteAsync<T>(
            Func<Task<T>> operation,
            CancellationToken cancellationToken = default
        ) =>
            operation();
    }

    /// <summary>
    ///     Recognizes a completed attempt without masking replacement evidence or lost ownership.
    /// </summary>
    /// <param name="guarded">Whether the caller supplies its supervised append ownership check.</param>
    /// <param name="hasReplacementAttempt">Whether a different pending attempt becomes visible.</param>
    /// <param name="hasLostOwnership">Whether the caller loses ownership during the fresh cursor read.</param>
    /// <param name="isCanceled">Whether the caller cancels during the fresh cursor read.</param>
    /// <returns>The asynchronous test.</returns>
    [Theory]
    [InlineData(false, false, false, false)]
    [InlineData(true, false, false, false)]
    [InlineData(false, true, false, false)]
    [InlineData(true, true, false, false)]
    [InlineData(true, false, true, false)]
    [InlineData(true, false, false, true)]
    public async Task RecoveryRechecksAnAttemptCommittedAfterItsSnapshotAsync(
        bool guarded,
        bool hasReplacementAttempt,
        bool hasLostOwnership,
        bool isCanceled
    )
    {
        using CancellationTokenSource watchdog = new(TimeSpan.FromSeconds(30));
        using CancellationTokenSource recoveryCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(watchdog.Token, TestContext.Current.CancellationToken);
        BrookKey key = new("test", "recovery-commit-race");
        CursorDocument? pending = new()
        {
            OriginalPosition = 10,
            Position = 12,
        };
        long committed = 10;
        string cursorETag = "cursor-10";
        string pendingETag = "attempt-a";
        int cursorReads = 0;
        bool ownershipLost = false;
        TimeoutException ownershipFailure = new("Append ownership lost during the commit read");
        TaskCompletionSource oldCommitDispatched = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseOldCommit = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Task oldCommit = Task.CompletedTask;
        List<string> reads = new();
        Mock<Container> container = new(MockBehavior.Strict);
        container.Setup(c => c.ReadItemAsync<CursorDocument>(
                It.IsAny<string>(),
                new(key.ToString()),
                null,
                It.IsAny<CancellationToken>()))
            .Returns(async (string id, PartitionKey _, ItemRequestOptions _, CancellationToken _) =>
            {
                reads.Add(id);
                if (id == "cursor-pending")
                {
                    if (pending is null)
                    {
                        throw new CosmosException("Pending attempt removed", HttpStatusCode.NotFound, 0, "test", 0);
                    }

                    Mock<ItemResponse<CursorDocument>> pendingResponse = new();
                    pendingResponse.SetupGet(r => r.Resource).Returns(pending);
                    pendingResponse.SetupGet(r => r.ETag).Returns(pendingETag);
                    return pendingResponse.Object;
                }

                cursorReads++;
                if (cursorReads == 3)
                {
                    releaseOldCommit.TrySetResult();
                    await oldCommit.WaitAsync(watchdog.Token);
                    if (hasReplacementAttempt)
                    {
                        pending = new()
                        {
                            OriginalPosition = 12,
                            Position = 13,
                        };
                        pendingETag = "attempt-b";
                    }

                    ownershipLost = hasLostOwnership;
                    if (isCanceled)
                    {
                        await recoveryCancellation.CancelAsync();
                    }
                }

                Mock<ItemResponse<CursorDocument>> response = new();
                response.SetupGet(r => r.Resource)
                    .Returns(
                        new CursorDocument
                        {
                            Position = committed,
                        });
                response.SetupGet(r => r.ETag).Returns(cursorETag);
                return response.Object;
            });
        container.Setup(c => c.ReadItemAsync<EventDocument>(
                It.IsIn("11", "12"),
                new(key.ToString()),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<ItemResponse<EventDocument>>());
        string? pendingCondition = null;
        string? cursorCondition = null;
        CursorDocument? candidate = null;
        Mock<TransactionalBatch> batch = new(MockBehavior.Strict);
        container.Setup(c => c.CreateTransactionalBatch(new(key.ToString()))).Returns(batch.Object);
        batch.Setup(b => b.DeleteItem("cursor-pending", It.IsAny<TransactionalBatchItemRequestOptions>()))
            .Callback<string, TransactionalBatchItemRequestOptions>((_, options) =>
                pendingCondition = options.IfMatchEtag)
            .Returns(batch.Object);
        batch.Setup(b => b.ReplaceItem(
                "cursor",
                It.IsAny<CursorDocument>(),
                It.IsAny<TransactionalBatchItemRequestOptions>()))
            .Callback<string, CursorDocument, TransactionalBatchItemRequestOptions>((_, document, options) =>
            {
                candidate = document;
                cursorCondition = options.IfMatchEtag;
            })
            .Returns(batch.Object);
        batch.Setup(b => b.ExecuteAsync(It.IsAny<CancellationToken>()))
            .Returns(async (CancellationToken token) =>
            {
                oldCommitDispatched.TrySetResult();
                await releaseOldCommit.Task.WaitAsync(token);
                bool canCommit = (pendingCondition == pendingETag) && (cursorCondition == cursorETag);
                if (canCommit)
                {
                    committed = candidate!.Position;
                    cursorETag = "cursor-12";
                    pending = null;
                }

                Mock<TransactionalBatchResponse> response = new();
                response.SetupGet(r => r.IsSuccessStatusCode).Returns(canCommit);
                response.SetupGet(r => r.StatusCode)
                    .Returns(canCommit ? HttpStatusCode.OK : HttpStatusCode.PreconditionFailed);
                return response.Object;
            });
        Mock<IMapper<CursorDocument, CursorStorageModel>> mapper = new();
        mapper.Setup(m => m.Map(It.IsAny<CursorDocument>()))
            .Returns((CursorDocument document) => new()
            {
                Position = new(document.Position),
                OriginalPosition = document.OriginalPosition is { } original ? new(original) : null,
            });
        DirectRetryPolicy retry = new();
        CosmosRepository repository = new(
            container.Object,
            retry,
            mapper.Object,
            Mock.Of<IMapper<EventDocument, EventStorageModel>>());
        Mock<IDistributedLock> writerLock = new();
        writerLock.Setup(l => l.RenewAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        Mock<IDistributedLockManager> lockManager = new(MockBehavior.Strict);
        lockManager.Setup(l => l.AcquireLockAsync(key.ToString(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(writerLock.Object);
        BrookRecoveryService recovery = new(
            repository,
            retry,
            lockManager.Object,
            Options.Create(new BrookStorageOptions()),
            NullLogger<BrookRecoveryService>.Instance);
        Action guard = () =>
        {
            if (ownershipLost)
            {
                throw ownershipFailure;
            }

            recoveryCancellation.Token.ThrowIfCancellationRequested();
        };
        oldCommit = repository.CommitCursorPositionAsync(key, 12, "attempt-a", watchdog.Token);
        await oldCommitDispatched.Task.WaitAsync(watchdog.Token);
        try
        {
            Func<Task<BrookPosition>> recover = async () => guarded
                ? await recovery.GetOrRecoverCursorPositionAsync(key, guard, recoveryCancellation.Token)
                : await recovery.GetOrRecoverCursorPositionAsync(key, recoveryCancellation.Token);
            if (hasLostOwnership)
            {
                Assert.Same(ownershipFailure, await Assert.ThrowsAsync<TimeoutException>(recover));
            }
            else if (isCanceled)
            {
                OperationCanceledException failure = await Assert.ThrowsAsync<OperationCanceledException>(recover);
                Assert.Equal(recoveryCancellation.Token, failure.CancellationToken);
            }
            else if (hasReplacementAttempt)
            {
                InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(recover);
                Assert.Contains("does not advance", failure.Message, StringComparison.Ordinal);
            }
            else
            {
                BrookPosition result = await recover();
                Assert.Equal(12, result.Value);
                Assert.Equal("cursor-pending", reads[^2]);
                Assert.Equal("cursor", reads[^1]);
            }
        }
        finally
        {
            releaseOldCommit.TrySetResult();
            await oldCommit.WaitAsync(watchdog.Token);
        }

        Assert.Equal(12, committed);
        Assert.Equal("cursor-12", cursorETag);
        if (hasReplacementAttempt)
        {
            Assert.NotNull(pending);
            Assert.Equal(13, pending.Position);
            Assert.Equal("attempt-b", pendingETag);
        }
        else
        {
            Assert.Null(pending);
        }

        container.Verify(c => c.CreateTransactionalBatch(new(key.ToString())), Times.Once);
        batch.Verify(b => b.ExecuteAsync(It.IsAny<CancellationToken>()), Times.Once);
        container.Verify(
            c => c.DeleteItemAsync<EventDocument>(
                It.IsAny<string>(),
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        writerLock.Verify(l => l.DisposeAsync(), guarded ? Times.Never() : Times.Once());
        lockManager.Verify(
            l => l.AcquireLockAsync(key.ToString(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            guarded ? Times.Never() : Times.Once());
    }
}