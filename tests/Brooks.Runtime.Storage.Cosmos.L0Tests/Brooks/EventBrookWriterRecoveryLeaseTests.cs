using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Batching;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Brooks;
using Mississippi.Brooks.Runtime.Storage.Cosmos.L0Tests.Locking;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Locking;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Storage;
using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Common.Runtime.Storage.Abstractions.Retry;

using Moq;


namespace Mississippi.Brooks.Runtime.Storage.Cosmos.L0Tests.Brooks;

/// <summary>
///     Composes writer, recovery and repository guards without changing standalone recovery ownership decisions.
/// </summary>
public sealed class EventBrookWriterRecoveryLeaseTests
{
    /// <summary>
    ///     Append-owned recovery observes its deadline after a held event read while the physical lease remains valid.
    /// </summary>
    /// <param name="isComplete">Whether the held read reports an existing event.</param>
    /// <returns>The asynchronous test.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RecoveryChecksAppendDeadlineBeforeFurtherDispatchAsync(
        bool isComplete
    )
    {
        BrookKey key = new("test", "append-recovery");
        LeaseTestTimeProvider clock = new(new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero))
        {
            SuppressDeadline = true,
        };
        TaskCompletionSource storageEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource finishStorage = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource renewalEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource finishRenewal = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource readCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Mock<IDistributedLock> appendLease = new();
        appendLease.SetupSequence(l => l.RenewAsync(true, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Returns(async () =>
            {
                renewalEntered.TrySetResult();
                await finishRenewal.Task.WaitAsync(CancellationToken.None);
            });
        Mock<IDistributedLockManager> locks = new(MockBehavior.Strict);
        locks.Setup(l => l.AcquireLockAsync(key.ToString(), TimeSpan.FromSeconds(15), It.IsAny<CancellationToken>()))
            .ReturnsAsync(appendLease.Object);
        long recoveryStarted = -1;
        Mock<Container> container = new(MockBehavior.Strict);
        CursorDocument cursor = new()
        {
            Id = "cursor",
            Position = 0,
        };
        Mock<ItemResponse<CursorDocument>> cursorResponse = new();
        cursorResponse.SetupGet(r => r.Resource).Returns(cursor);
        cursorResponse.SetupGet(r => r.ETag).Returns("cursor-version");
        container.Setup(c => c.ReadItemAsync<CursorDocument>(
                "cursor",
                It.IsAny<PartitionKey>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cursorResponse.Object);
        CursorDocument pending = new()
        {
            Id = "cursor-pending",
            Position = 2,
            OriginalPosition = 0,
        };
        Mock<ItemResponse<CursorDocument>> pendingResponse = new();
        pendingResponse.SetupGet(r => r.Resource).Returns(pending);
        pendingResponse.SetupGet(r => r.ETag).Returns("pending-attempt");
        container.Setup(c => c.ReadItemAsync<CursorDocument>(
                "cursor-pending",
                It.IsAny<PartitionKey>(),
                null,
                It.IsAny<CancellationToken>()))
            .Callback(() => recoveryStarted = clock.GetTimestamp())
            .ReturnsAsync(pendingResponse.Object);
        container.Setup(c => c.ReadItemAsync<EventDocument>(
                "1",
                It.IsAny<PartitionKey>(),
                null,
                It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                storageEntered.TrySetResult();
                await finishStorage.Task.WaitAsync(CancellationToken.None);
                readCompleted.TrySetResult();
                if (!isComplete)
                {
                    throw new CosmosException("Missing event", HttpStatusCode.NotFound, 0, "missing", 0);
                }

                return Mock.Of<ItemResponse<EventDocument>>();
            });
        Mock<IRetryPolicy> retry = new();
        retry.Setup(r => r.ExecuteAsync(It.IsAny<Func<Task<CursorStorageModel?>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<Task<CursorStorageModel?>> operation, CancellationToken _) => operation());
        retry.Setup(r => r.ExecuteAsync(It.IsAny<Func<Task<bool>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<Task<bool>> operation, CancellationToken _) => operation());
        Mock<IMapper<CursorDocument, CursorStorageModel>> cursorMapper = new();
        cursorMapper.Setup(m => m.Map(pending))
            .Returns(
                new CursorStorageModel
                {
                    Position = new(2),
                    OriginalPosition = new(0),
                });
        cursorMapper.Setup(m => m.Map(cursor))
            .Returns(
                new CursorStorageModel
                {
                    Position = new(0),
                });
        CosmosRepository repository = new(
            container.Object,
            retry.Object,
            cursorMapper.Object,
            Mock.Of<IMapper<EventDocument, EventStorageModel>>());
        IOptions<BrookStorageOptions> options = Options.Create(
            new BrookStorageOptions
            {
                LeaseDurationSeconds = 15,
                LeaseRenewalThresholdSeconds = 5,
            });
        BrookRecoveryService recovery = new(
            repository,
            retry.Object,
            locks.Object,
            options,
            NullLogger<BrookRecoveryService>.Instance);
        EventBrookWriter writer = new(
            repository,
            locks.Object,
            new BatchSizeEstimator(),
            retry.Object,
            options,
            Mock.Of<IMapper<BrookEvent, EventStorageModel>>(),
            recovery,
            NullLogger<EventBrookWriter>.Instance,
            clock);
        long initialTimestamp = clock.GetTimestamp();
        using CancellationTokenSource watchdog =
            CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        watchdog.CancelAfter(TimeSpan.FromSeconds(10));
        Task<BrookPosition> append = writer.AppendEventsAsync(
            key,
            [
                new()
                {
                    Id = "new-event",
                },
            ],
            null,
            watchdog.Token);
        try
        {
            await storageEntered.Task.WaitAsync(watchdog.Token);
            Assert.Equal(initialTimestamp, recoveryStarted);
            clock.Advance(TimeSpan.FromSeconds(5));
            await renewalEntered.Task.WaitAsync(watchdog.Token);
            clock.Advance(TimeSpan.FromSeconds(9));
            Assert.Equal(TimeSpan.FromSeconds(14), clock.GetElapsedTime(recoveryStarted));
            Assert.True(clock.GetElapsedTime(recoveryStarted) < TimeSpan.FromSeconds(15));
            finishStorage.TrySetResult();
            await readCompleted.Task.WaitAsync(watchdog.Token);
            Assert.False(append.IsCompleted);
            appendLease.Verify(l => l.DisposeAsync(), Times.Never);
        }
        finally
        {
            finishStorage.TrySetResult();
            finishRenewal.TrySetResult();
            await Assert.ThrowsAsync<TimeoutException>(() => append.WaitAsync(CancellationToken.None));
        }

        container.Verify(c => c.CreateTransactionalBatch(It.IsAny<PartitionKey>()), Times.Never);
        container.Verify(
            c => c.UpsertItemAsync(
                It.IsAny<CursorDocument>(),
                It.IsAny<PartitionKey>(),
                null,
                It.IsAny<CancellationToken>()),
            Times.Never);
        container.Verify(
            c => c.DeleteItemAsync<CursorDocument>(
                It.IsAny<string>(),
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        container.Verify(
            c => c.DeleteItemAsync<EventDocument>(
                It.IsAny<string>(),
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        container.Verify(
            c => c.ReadItemAsync<EventDocument>("2", It.IsAny<PartitionKey>(), null, It.IsAny<CancellationToken>()),
            Times.Never);
        container.Verify(
            c => c.CreateItemAsync(
                It.IsAny<EventDocument>(),
                It.IsAny<PartitionKey>(),
                null,
                It.IsAny<CancellationToken>()),
            Times.Never);
        locks.Verify(
            l => l.AcquireLockAsync(key.ToString(), TimeSpan.FromSeconds(15), It.IsAny<CancellationToken>()),
            Times.Once);
        locks.VerifyNoOtherCalls();
        appendLease.Verify(l => l.DisposeAsync(), Times.Once);
        Assert.Equal(0, clock.ActiveTimers);
    }

    /// <summary>
    ///     An unchanged standalone recovery signature continues selecting the old repository contract.
    /// </summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task StandaloneRecoveryRetainsOldRoutingAsync()
    {
        BrookKey key = new("test", "standalone-recovery");
        Mock<ICosmosRepository> repository = new(MockBehavior.Strict);
        repository.Setup(r => r.GetCursorDocumentAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new CursorStorageModel
                {
                    Position = new(3),
                });
        repository.Setup(r => r.GetPendingCursorDocumentAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CursorStorageModel?)null);
        Mock<IRetryPolicy> retry = new(MockBehavior.Strict);
        retry.Setup(r => r.ExecuteAsync(It.IsAny<Func<Task<CursorStorageModel?>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<Task<CursorStorageModel?>> operation, CancellationToken _) => operation());
        Mock<IDistributedLockManager> locks = new(MockBehavior.Strict);
        BrookRecoveryService recovery = new(
            repository.Object,
            retry.Object,
            locks.Object,
            Options.Create(new BrookStorageOptions()),
            NullLogger<BrookRecoveryService>.Instance);
        BrookPosition result =
            await recovery.GetOrRecoverCursorPositionAsync(key, TestContext.Current.CancellationToken);
        Assert.Equal(3, result.Value);
        repository.Verify(r => r.GetCursorDocumentAsync(key, It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(r => r.GetPendingCursorDocumentAsync(key, It.IsAny<CancellationToken>()), Times.Once);
        repository.VerifyNoOtherCalls();
        locks.VerifyNoOtherCalls();
    }
}