using System.Collections.Immutable;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.Core.Abstractions.Mapping;
using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Cosmos.Abstractions;
using Mississippi.EventSourcing.Cosmos.Batching;
using Mississippi.EventSourcing.Cosmos.Brooks;
using Mississippi.EventSourcing.Cosmos.Locking;
using Mississippi.EventSourcing.Cosmos.Retry;
using Mississippi.EventSourcing.Cosmos.Storage;

using Moq;


namespace Mississippi.EventSourcing.Cosmos.Tests.Brooks;

/// <summary>
///     Tests for rollback behavior in <see cref="EventBrookAppender" />.
/// </summary>
public class EventBrookAppenderRollbackTests
{
    /// <summary>
    ///     When an append batch fails mid-stream, rollback should attempt cleanup and aggregate issues.
    /// </summary>
    [Fact]
    public async Task AppendLargeBatchAsync_RollsBack_OnFailure_AndAggregatesIssues()
    {
        // Arrange
        BrookKey key = new("t", "rb1");
        BrookStorageOptions opts = new()
        {
            MaxEventsPerBatch = 1, // force large-batch path
            MaxRequestSizeBytes = 1_000_000,
            LeaseDurationSeconds = 5,
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
            .Returns(async (
                Func<Task<bool>> op,
                CancellationToken _
            ) => await op());
        Mock<IMapper<BrookEvent, EventStorageModel>> mapper = new();
        mapper.Setup(m => m.Map(It.IsAny<BrookEvent>()))
            .Returns<BrookEvent>(e => new()
            {
                EventId = e.Id,
                EventType = e.Type,
                Data = e.Data.ToArray(),
                DataContentType = e.DataContentType,
                Source = e.Source,
                Time = e.Time ?? DateTimeOffset.UtcNow,
            });
        Mock<IBrookRecoveryService> recovery = new();
        recovery.Setup(r => r.GetOrRecoverHeadPositionAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrookPosition(0));
        Mock<ILogger<EventBrookAppender>> logger = new();
        EventBrookAppender sut = new(
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
                Data = ImmutableArray.Create(new byte[] { 1 }),
                DataContentType = "application/octet-stream",
            },
            new()
            {
                Id = "2",
                Data = ImmutableArray.Create(new byte[] { 2 }),
                DataContentType = "application/octet-stream",
            },
            new()
            {
                Id = "3",
                Data = ImmutableArray.Create(new byte[] { 3 }),
                DataContentType = "application/octet-stream",
            },
        };

        // CreatePendingHead succeeds
        repo.Setup(r => r.CreatePendingHeadAsync(key, new(0), 3, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        int appendCalls = 0;
        repo.Setup(r => r.AppendEventBatchAsync(
                key,
                It.IsAny<IReadOnlyList<EventStorageModel>>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()))
            .Returns((
                BrookKey _,
                IReadOnlyList<EventStorageModel> __,
                long ___,
                CancellationToken ____
            ) =>
            {
                appendCalls++;
                if (appendCalls == 2)
                {
                    throw new InvalidOperationException("boom");
                }

                return Task.CompletedTask;
            });

        // CommitHead shouldn't be reached, but safe default
        repo.Setup(r => r.CommitHeadPositionAsync(key, 3, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Rollback: simulate mixed failures and leftovers
        repo.Setup(r => r.DeleteEventAsync(key, It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("delete failed"));
        repo.Setup(r => r.DeletePendingHeadAsync(key, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("delete pending failed"));
        repo.Setup(r => r.EventExistsAsync(key, It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<AggregateException>(() => sut.AppendEventsAsync(key, events, null));
    }

    /// <summary>
    ///     When initial append fails with no processed events, rollback should clean up pending head without aggregate
    ///     exception.
    /// </summary>
    [Fact]
    public async Task AppendLargeBatchAsync_RollsBack_CleansUp_WhenDeletesSucceed()
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
            .Returns<Func<Task<bool>>, CancellationToken>(async (
                op,
                _
            ) => await op());
        Mock<IMapper<BrookEvent, EventStorageModel>> mapper = new();
        mapper.Setup(m => m.Map(It.IsAny<BrookEvent>())).Returns(new EventStorageModel());
        Mock<IBrookRecoveryService> recovery = new();
        recovery.Setup(r => r.GetOrRecoverHeadPositionAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrookPosition(0));
        Mock<ILogger<EventBrookAppender>> logger = new();
        EventBrookAppender sut = new(
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
        repo.Setup(r => r.CreatePendingHeadAsync(key, new(0), 2, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        // Throw on first append to force rollback with processedEvents == 0
        repo.Setup(r => r.AppendEventBatchAsync(
                key,
                It.IsAny<IReadOnlyList<EventStorageModel>>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("fail early"));
        repo.Setup(r => r.DeletePendingHeadAsync(key, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        repo.Setup(r => r.EventExistsAsync(key, It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.AppendEventsAsync(key, events, null));

        // Assert: no specific verifications, just that rollback completed without AggregateException
        repo.Verify(r => r.DeletePendingHeadAsync(key, It.IsAny<CancellationToken>()), Times.Once);
    }
}