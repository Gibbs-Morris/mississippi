using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Batching;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Brooks;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Locking;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Storage;
using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Common.Runtime.Storage.Abstractions.Retry;

using Moq;


namespace Mississippi.Brooks.Runtime.Storage.Cosmos.L0Tests.Brooks;

/// <summary>
///     Tests that uncertain append failures retain recovery evidence.
/// </summary>
public sealed class EventBrookWriterRollbackTests
{
    private static readonly DateTimeOffset MapperFallbackTime = new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);

    /// <summary>
    ///     A failure after one acknowledged batch retains that batch and pending metadata.
    /// </summary>
    /// <returns>
    ///     A task that represents the asynchronous test operation.
    /// </returns>
    [Fact]
    public async Task AppendLargeBatchRetainsEventsAndPendingOnLaterFailureAsync()
    {
        // Arrange
        BrookKey key = new("t", "rb1");
        BrookStorageOptions opts = new()
        {
            MaxEventsPerBatch = 1, // force large-batch path
            MaxRequestSizeBytes = 1_000_000,
            LeaseDurationSeconds = 15,
            LeaseRenewalThresholdSeconds = 1,
        };
        Mock<ICosmosRepository> repo = new();
        Mock<IDistributedLockManager> lockMgr = new();
        Mock<IDistributedLock> lockInstance = new();
        lockMgr.Setup(m => m.AcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lockInstance.Object);
        IBatchSizeEstimator sizeEstimator = new BatchSizeEstimator();
        Mock<IRetryPolicy> retry = new();
        retry.Setup(r => r.ExecuteAsync(It.IsAny<Func<Task<bool>>>(), It.IsAny<CancellationToken>()))
            .Returns(async (Func<Task<bool>> op, CancellationToken _) => await op());
        Mock<IMapper<BrookEvent, EventStorageModel>> mapper = new();
        mapper.Setup(m => m.Map(It.IsAny<BrookEvent>()))
            .Returns<BrookEvent>(e => new()
            {
                EventId = e.Id,
                EventType = e.EventType,
                Data = e.Data.ToArray(),
                DataContentType = e.DataContentType,
                Source = e.Source,
                Time = e.Time ?? MapperFallbackTime,
            });
        Mock<IBrookRecoveryService> recovery = new();
        recovery.Setup(r => r.GetOrRecoverCursorPositionAsync(key, It.IsAny<Action>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrookPosition(0));
        Mock<ILogger<EventBrookWriter>> logger = new();
        EventBrookWriter sut = new(
            repo.Object,
            lockMgr.Object,
            sizeEstimator,
            retry.Object,
            Options.Create(opts),
            mapper.Object,
            recovery.Object,
            logger.Object);

        // events -> 3 batches of 1
        List<BrookEvent> events = new()
        {
            new()
            {
                Id = "1",
                Data = ImmutableArray.Create<byte>(1),
                DataContentType = "application/octet-stream",
            },
            new()
            {
                Id = "2",
                Data = ImmutableArray.Create<byte>(2),
                DataContentType = "application/octet-stream",
            },
            new()
            {
                Id = "3",
                Data = ImmutableArray.Create<byte>(3),
                DataContentType = "application/octet-stream",
            },
        };

        // CreatePendingCursor succeeds
        repo.Setup(r => r.CreatePendingCursorAsync(key, new(0), 3, It.IsAny<Action>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("attempt-etag");
        int appendCalls = 0;
        InvalidOperationException failure = new("boom");
        repo.Setup(r => r.AppendEventBatchAsync(
                key,
                It.IsAny<IReadOnlyList<EventStorageModel>>(),
                It.IsAny<long>(),
                It.IsAny<Action>(),
                It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                appendCalls++;
                if (appendCalls == 2)
                {
                    throw failure;
                }

                return Task.CompletedTask;
            });

        // CommitCursor shouldn't be reached, but safe default
        repo.Setup(r => r.CommitCursorPositionAsync(
                key,
                3,
                "attempt-etag",
                It.IsAny<Action>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act & Assert
        InvalidOperationException thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.AppendEventsAsync(key, events, null, TestContext.Current.CancellationToken));
        Assert.Same(failure, thrown);
        Assert.Equal(2, appendCalls);
        repo.Verify(
            r => r.DeleteEventAsync(key, It.IsAny<long>(), It.IsAny<Action>(), It.IsAny<CancellationToken>()),
            Times.Never);
        repo.Verify(
            r => r.DeletePendingCursorAsync(key, It.IsAny<string>(), It.IsAny<Action>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    ///     A failed first append can still complete later, so pending metadata must remain.
    /// </summary>
    /// <param name="isCanceled">Whether the failed response reports cancellation.</param>
    /// <returns>
    ///     A task that represents the asynchronous test operation.
    /// </returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AppendLargeBatchRetainsPendingAfterFirstFailureAsync(
        bool isCanceled
    )
    {
        // Arrange
        BrookKey key = new("t", "rb2");
        BrookStorageOptions opts = new()
        {
            MaxEventsPerBatch = 1,
            MaxRequestSizeBytes = 1_000_000,
        };
        Mock<ICosmosRepository> repo = new();
        Mock<IDistributedLockManager> lockMgr = new();
        Mock<IDistributedLock> lockInstance = new();
        lockMgr.Setup(m => m.AcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lockInstance.Object);
        IBatchSizeEstimator sizeEstimator = new BatchSizeEstimator();
        Mock<IRetryPolicy> retry = new();
        retry.Setup(r => r.ExecuteAsync(It.IsAny<Func<Task<bool>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task<bool>>, CancellationToken>(async (op, _) => await op());
        Mock<IMapper<BrookEvent, EventStorageModel>> mapper = new();
        mapper.Setup(m => m.Map(It.IsAny<BrookEvent>())).Returns(new EventStorageModel());
        Mock<IBrookRecoveryService> recovery = new();
        recovery.Setup(r => r.GetOrRecoverCursorPositionAsync(key, It.IsAny<Action>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrookPosition(0));
        Mock<ILogger<EventBrookWriter>> logger = new();
        logger.Setup(l => l.IsEnabled(LogLevel.Error)).Returns(true);
        EventBrookWriter sut = new(
            repo.Object,
            lockMgr.Object,
            sizeEstimator,
            retry.Object,
            Options.Create(opts),
            mapper.Object,
            recovery.Object,
            logger.Object);
        List<BrookEvent> events = new()
        {
            new(),
            new(),
        };
        repo.Setup(r => r.CreatePendingCursorAsync(key, new(0), 2, It.IsAny<Action>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("attempt-etag");

        // A failed acknowledgement does not prove the Cosmos create failed.
        Exception failure = isCanceled
            ? new OperationCanceledException("request canceled after send")
            : new InvalidOperationException("acknowledgement lost");
        TaskCompletionSource<bool> lateCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        bool isEventCreated = false;
        Task lateWrite = Task.Run(
            async () =>
            {
                await lateCompletion.Task.WaitAsync(TestContext.Current.CancellationToken);
                isEventCreated = true;
            },
            TestContext.Current.CancellationToken);
        repo.Setup(r => r.AppendEventBatchAsync(
                key,
                It.IsAny<IReadOnlyList<EventStorageModel>>(),
                It.IsAny<long>(),
                It.IsAny<Action>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromException(failure));

        // Act
        Exception? thrown;
        try
        {
            thrown = await Record.ExceptionAsync(() => sut.AppendEventsAsync(
                key,
                events,
                null,
                TestContext.Current.CancellationToken));
        }
        finally
        {
            lateCompletion.TrySetResult(true);
            await lateWrite.WaitAsync(TestContext.Current.CancellationToken);
        }

        Assert.Same(failure, thrown);
        Assert.True(isEventCreated);
        repo.Verify(
            r => r.DeletePendingCursorAsync(key, It.IsAny<string>(), It.IsAny<Action>(), It.IsAny<CancellationToken>()),
            Times.Never);
        repo.Verify(
            r => r.DeleteEventAsync(key, It.IsAny<long>(), It.IsAny<Action>(), It.IsAny<CancellationToken>()),
            Times.Never);
        logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.Is<EventId>(id => id.Id == 1015),
                It.IsAny<It.IsAnyType>(),
                failure,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}