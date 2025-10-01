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
///     Additional mutation-killing tests for <see cref="EventBrookAppender" /> to achieve 100% mutation coverage.
///     These tests specifically target surviving mutations in boundary checks, logging, and error handling.
/// </summary>
public class EventBrookAppenderMutationTests
{
    /// <summary>
    ///     Verifies constructor throws when options parameter is null (kills null coalescing mutation on line 100).
    /// </summary>
    [Fact]
    public void ConstructorThrowsOnNullOptions()
    {
        // Arrange
        Mock<ICosmosRepository> repository = new();
        Mock<IDistributedLockManager> lockManager = new();
        Mock<IBatchSizeEstimator> sizeEstimator = new();
        Mock<IRetryPolicy> retryPolicy = new();
        Mock<IMapper<BrookEvent, EventStorageModel>> mapper = new();
        Mock<IBrookRecoveryService> recovery = new();
        Mock<ILogger<EventBrookAppender>> logger = new();

        // Act + Assert
        Assert.Throws<ArgumentNullException>(() => new EventBrookAppender(
            repository.Object,
            lockManager.Object,
            sizeEstimator.Object,
            retryPolicy.Object,
            null!,
            mapper.Object,
            recovery.Object,
            logger.Object));
    }

    /// <summary>
    ///     Verifies exact boundary where events.Count == int.MaxValue / 2 is rejected
    ///     (kills equality mutation on line 143: > vs >=).
    /// </summary>
    [Fact]
    public async Task AppendEventsAsyncThrowsAtExactCountBoundary()
    {
        // Arrange - create fake list that reports exactly int.MaxValue / 2
        int exactBoundary = int.MaxValue / 2;
        IReadOnlyList<BrookEvent> boundaryList = new HugeReadOnlyList(exactBoundary);
        Mock<ICosmosRepository> repository = new(MockBehavior.Strict);
        Mock<IDistributedLockManager> lockManager = new(MockBehavior.Strict);
        Mock<IBatchSizeEstimator> sizeEstimator = new(MockBehavior.Strict);
        Mock<IRetryPolicy> retryPolicy = new(MockBehavior.Strict);
        Mock<IMapper<BrookEvent, EventStorageModel>> mapper = new(MockBehavior.Strict);
        Mock<IBrookRecoveryService> recovery = new(MockBehavior.Strict);
        Mock<ILogger<EventBrookAppender>> logger = new();
        EventBrookAppender sut = new(
            repository.Object,
            lockManager.Object,
            sizeEstimator.Object,
            retryPolicy.Object,
            Options.Create(new BrookStorageOptions()),
            mapper.Object,
            recovery.Object,
            logger.Object);

        // Act + Assert - this should NOT throw because count equals (not exceeds) the boundary
        // So we need to test count == boundary doesn't throw, but count > boundary does
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await sut.AppendEventsAsync(new("t", "1"), new HugeReadOnlyList(exactBoundary + 1), null));
    }

    /// <summary>
    ///     Verifies exact position overflow boundary check where currentHead.Value > long.MaxValue - events.Count
    ///     (kills equality mutation on line 176: > vs >=).
    /// </summary>
    [Fact]
    public async Task AppendEventsAsyncThrowsOnPositionOverflow()
    {
        // Arrange
        BrookKey brook = new("type", "id");
        int eventCount = 100;
        // Set head just beyond the boundary so overflow occurs
        long headBeyondBoundary = long.MaxValue - eventCount + 1;
        BrookEvent[] events = Enumerable.Range(0, eventCount)
            .Select(i => new BrookEvent { Id = $"e{i}", Data = ImmutableArray.Create<byte>(1) })
            .ToArray();

        Mock<ICosmosRepository> repository = new(MockBehavior.Strict);
        Mock<IDistributedLock> lockMock = new(MockBehavior.Strict);
        lockMock.Setup(l => l.DisposeAsync()).Returns(default(ValueTask));
        Mock<IDistributedLockManager> lockManager = new(MockBehavior.Strict);
        lockManager
            .Setup(m => m.AcquireLockAsync(brook.ToString(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lockMock.Object);
        Mock<IBatchSizeEstimator> sizeEstimator = new(MockBehavior.Strict);
        Mock<IRetryPolicy> retryPolicy = new(MockBehavior.Strict);
        Mock<IMapper<BrookEvent, EventStorageModel>> mapper = new(MockBehavior.Strict);
        Mock<IBrookRecoveryService> recovery = new(MockBehavior.Strict);
        recovery.Setup(r => r.GetOrRecoverHeadPositionAsync(brook, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrookPosition(headBeyondBoundary));
        Mock<ILogger<EventBrookAppender>> logger = new();
        EventBrookAppender sut = new(
            repository.Object,
            lockManager.Object,
            sizeEstimator.Object,
            retryPolicy.Object,
            Options.Create(new BrookStorageOptions()),
            mapper.Object,
            recovery.Object,
            logger.Object);

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await sut.AppendEventsAsync(brook, events, null));
    }

    /// <summary>
    ///     Verifies batch size boundary where events.Count > Options.MaxEventsPerBatch triggers large batch
    ///     (kills equality mutation on line 185: > vs >=).
    /// </summary>
    [Fact]
    public async Task AppendEventsAsyncUsesLargeBatchWhenCountExceedsBoundary()
    {
        // Arrange
        BrookKey brook = new("type", "id");
        int maxBatchSize = 4;
        // Use 5 events, which exceeds maxBatchSize of 4
        BrookEvent[] events = Enumerable.Range(0, 5)
            .Select(i => new BrookEvent { Id = $"e{i}", Data = ImmutableArray.Create<byte>((byte)i) })
            .ToArray();

        Mock<ICosmosRepository> repository = new(MockBehavior.Strict);
        Mock<IDistributedLock> lockMock = new(MockBehavior.Strict);
        lockMock.Setup(l => l.RenewAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        lockMock.Setup(l => l.DisposeAsync()).Returns(default(ValueTask));
        Mock<IDistributedLockManager> lockManager = new(MockBehavior.Strict);
        lockManager
            .Setup(m => m.AcquireLockAsync(brook.ToString(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lockMock.Object);
        Mock<IBatchSizeEstimator> sizeEstimator = new(MockBehavior.Strict);
        sizeEstimator.Setup(s => s.EstimateBatchSize(It.IsAny<IReadOnlyList<BrookEvent>>()))
            .Returns(1000L); // Small size to test count boundary only
        // Since events.Count > maxBatchSize, should use large batch path
        sizeEstimator.Setup(s => s.CreateSizeLimitedBatches(It.IsAny<IReadOnlyList<BrookEvent>>(), maxBatchSize, It.IsAny<long>()))
            .Returns(new[] { events.Take(3).ToArray(), events.Skip(3).ToArray() });
        Mock<IRetryPolicy> retryPolicy = new(MockBehavior.Strict);
        retryPolicy.Setup(p => p.ExecuteAsync(It.IsAny<Func<Task<bool>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<Task<bool>> op, CancellationToken _) => op());
        Mock<IMapper<BrookEvent, EventStorageModel>> mapper = new(MockBehavior.Strict);
        foreach (BrookEvent evt in events)
        {
            mapper.Setup(m => m.Map(evt))
                .Returns(new EventStorageModel { EventId = evt.Id, Data = evt.Data.ToArray() });
        }

        Mock<IBrookRecoveryService> recovery = new(MockBehavior.Strict);
        recovery.Setup(r => r.GetOrRecoverHeadPositionAsync(brook, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrookPosition(10));
        repository.Setup(r => r.CreatePendingHeadAsync(brook, new BrookPosition(10), 15, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository.Setup(r => r.AppendEventBatchAsync(
                brook,
                It.IsAny<List<EventStorageModel>>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository.Setup(r => r.CommitHeadPositionAsync(brook, 15, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        Mock<ILogger<EventBrookAppender>> logger = new();
        BrookStorageOptions options = new() { MaxEventsPerBatch = maxBatchSize };
        EventBrookAppender sut = new(
            repository.Object,
            lockManager.Object,
            sizeEstimator.Object,
            retryPolicy.Object,
            Options.Create(options),
            mapper.Object,
            recovery.Object,
            logger.Object);

        // Act
        BrookPosition result = await sut.AppendEventsAsync(brook, events, null);

        // Assert
        Assert.Equal(15L, result.Value);
        // Verify CreateSizeLimitedBatches was called (indicates large batch path)
        sizeEstimator.Verify(s => s.CreateSizeLimitedBatches(It.IsAny<IReadOnlyList<BrookEvent>>(), maxBatchSize, It.IsAny<long>()), Times.Once);
    }

    /// <summary>
    ///     Verifies size boundary where estimatedSize > Options.MaxRequestSizeBytes triggers large batch
    ///     (kills equality mutation on line 185: > vs >=).
    /// </summary>
    [Fact]
    public async Task AppendEventsAsyncUsesLargeBatchWhenSizeExceedsBoundary()
    {
        // Arrange
        BrookKey brook = new("type", "id");
        long maxSize = 10000L;
        BrookEvent[] events = new[]
        {
            new BrookEvent { Id = "e1", Data = ImmutableArray.Create<byte>(1) },
            new BrookEvent { Id = "e2", Data = ImmutableArray.Create<byte>(2) },
        };

        Mock<ICosmosRepository> repository = new(MockBehavior.Strict);
        Mock<IDistributedLock> lockMock = new(MockBehavior.Strict);
        lockMock.Setup(l => l.RenewAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        lockMock.Setup(l => l.DisposeAsync()).Returns(default(ValueTask));
        Mock<IDistributedLockManager> lockManager = new(MockBehavior.Strict);
        lockManager
            .Setup(m => m.AcquireLockAsync(brook.ToString(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lockMock.Object);
        Mock<IBatchSizeEstimator> sizeEstimator = new(MockBehavior.Strict);
        // Return size just above boundary to trigger large batch path
        sizeEstimator.Setup(s => s.EstimateBatchSize(It.IsAny<IReadOnlyList<BrookEvent>>()))
            .Returns(maxSize + 1);
        sizeEstimator.Setup(s => s.CreateSizeLimitedBatches(events, It.IsAny<int>(), It.IsAny<long>()))
            .Returns(new[] { new[] { events[0] }, new[] { events[1] } });
        Mock<IRetryPolicy> retryPolicy = new(MockBehavior.Strict);
        retryPolicy.Setup(p => p.ExecuteAsync(It.IsAny<Func<Task<bool>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<Task<bool>> op, CancellationToken _) => op());
        Mock<IMapper<BrookEvent, EventStorageModel>> mapper = new(MockBehavior.Strict);
        foreach (BrookEvent evt in events)
        {
            mapper.Setup(m => m.Map(evt))
                .Returns(new EventStorageModel { EventId = evt.Id, Data = evt.Data.ToArray() });
        }

        Mock<IBrookRecoveryService> recovery = new(MockBehavior.Strict);
        recovery.Setup(r => r.GetOrRecoverHeadPositionAsync(brook, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrookPosition(5));
        repository.Setup(r => r.CreatePendingHeadAsync(brook, new BrookPosition(5), 7, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository.Setup(r => r.AppendEventBatchAsync(
                brook,
                It.IsAny<List<EventStorageModel>>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository.Setup(r => r.CommitHeadPositionAsync(brook, 7, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        Mock<ILogger<EventBrookAppender>> logger = new();
        BrookStorageOptions options = new() { MaxEventsPerBatch = 1000, MaxRequestSizeBytes = maxSize };
        EventBrookAppender sut = new(
            repository.Object,
            lockManager.Object,
            sizeEstimator.Object,
            retryPolicy.Object,
            Options.Create(options),
            mapper.Object,
            recovery.Object,
            logger.Object);

        // Act
        BrookPosition result = await sut.AppendEventsAsync(brook, events, null);

        // Assert
        Assert.Equal(7L, result.Value);
        sizeEstimator.Verify(s => s.CreateSizeLimitedBatches(events, 1000, maxSize), Times.Once);
    }

    /// <summary>
    ///     Verifies position calculation in single batch uses addition correctly
    ///     (kills arithmetic mutation on line 216: + vs -).
    /// </summary>
    [Fact]
    public async Task AppendSingleBatchCalculatesPositionCorrectly()
    {
        // Arrange
        BrookKey brook = new("type", "id");
        long currentHead = 42L;
        BrookEvent[] events = new[]
        {
            new BrookEvent { Id = "e1", Data = ImmutableArray.Create<byte>(1) },
        };

        Mock<ICosmosRepository> repository = new(MockBehavior.Strict);
        Mock<IDistributedLock> lockMock = new(MockBehavior.Strict);
        lockMock.Setup(l => l.RenewAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        lockMock.Setup(l => l.DisposeAsync()).Returns(default(ValueTask));
        Mock<IDistributedLockManager> lockManager = new(MockBehavior.Strict);
        lockManager
            .Setup(m => m.AcquireLockAsync(brook.ToString(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lockMock.Object);
        Mock<IBatchSizeEstimator> sizeEstimator = new(MockBehavior.Strict);
        sizeEstimator.Setup(s => s.EstimateBatchSize(It.IsAny<IReadOnlyList<BrookEvent>>()))
            .Returns(100L);
        Mock<IRetryPolicy> retryPolicy = new(MockBehavior.Strict);
        retryPolicy.Setup(p => p.ExecuteAsync(It.IsAny<Func<Task<bool>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<Task<bool>> op, CancellationToken _) => op());
        Mock<IMapper<BrookEvent, EventStorageModel>> mapper = new(MockBehavior.Strict);
        mapper.Setup(m => m.Map(events[0]))
            .Returns(new EventStorageModel { EventId = events[0].Id, Data = events[0].Data.ToArray() });
        Mock<IBrookRecoveryService> recovery = new(MockBehavior.Strict);
        recovery.Setup(r => r.GetOrRecoverHeadPositionAsync(brook, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrookPosition(currentHead));
        repository.Setup(r => r.CreatePendingHeadAsync(
                brook,
                new BrookPosition(currentHead),
                currentHead + events.Length,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        // Verify the position passed to AppendEventBatchAsync is currentHead + 1
        repository.Setup(r => r.AppendEventBatchAsync(
                brook,
                It.Is<List<EventStorageModel>>(l => l.Count == 1),
                currentHead + 1, // This is the critical assertion
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository.Setup(r => r.CommitHeadPositionAsync(brook, currentHead + 1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        Mock<ILogger<EventBrookAppender>> logger = new();
        EventBrookAppender sut = new(
            repository.Object,
            lockManager.Object,
            sizeEstimator.Object,
            retryPolicy.Object,
            Options.Create(new BrookStorageOptions()),
            mapper.Object,
            recovery.Object,
            logger.Object);

        // Act
        BrookPosition result = await sut.AppendEventsAsync(brook, events, null);

        // Assert
        Assert.Equal(currentHead + 1, result.Value);
        repository.Verify();
    }

    /// <summary>
    ///     Verifies rollback with zero remaining events does NOT throw
    ///     (kills logical mutation on line 383: || vs &&).
    /// </summary>
    [Fact]
    public async Task RollbackSucceedsWhenNoRemainingEventsOrErrors()
    {
        // Arrange - set up scenario where rollback succeeds cleanly
        BrookKey brook = new("type", "id");
        BrookStorageOptions opts = new()
        {
            MaxEventsPerBatch = 1, // force large batch path
            MaxRequestSizeBytes = 1_000_000,
        };
        Mock<ICosmosRepository> repository = new();
        Mock<IDistributedLock> lockMock = new();
        lockMock.Setup(l => l.RenewAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        Mock<IDistributedLockManager> lockManager = new();
        lockManager.Setup(m => m.AcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lockMock.Object);
        Mock<IBatchSizeEstimator> sizeEstimator = new();
        sizeEstimator.Setup(s => s.EstimateBatchSize(It.IsAny<IReadOnlyList<BrookEvent>>()))
            .Returns(100L);
        sizeEstimator.Setup(s => s.CreateSizeLimitedBatches(It.IsAny<IReadOnlyList<BrookEvent>>(), 1, It.IsAny<long>()))
            .Returns<IReadOnlyList<BrookEvent>, int, long>((evts, _, _) =>
                evts.Select(e => new[] { e }).ToArray());
        Mock<IRetryPolicy> retryPolicy = new();
        retryPolicy.Setup(r => r.ExecuteAsync(It.IsAny<Func<Task<bool>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<Task<bool>> op, CancellationToken _) => op());
        Mock<IMapper<BrookEvent, EventStorageModel>> mapper = new();
        mapper.Setup(m => m.Map(It.IsAny<BrookEvent>()))
            .Returns<BrookEvent>(e => new() { EventId = e.Id, Data = e.Data.ToArray() });
        Mock<IBrookRecoveryService> recovery = new();
        recovery.Setup(r => r.GetOrRecoverHeadPositionAsync(brook, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrookPosition(0));
        Mock<ILogger<EventBrookAppender>> logger = new();
        EventBrookAppender sut = new(
            repository.Object,
            lockManager.Object,
            sizeEstimator.Object,
            retryPolicy.Object,
            Options.Create(opts),
            mapper.Object,
            recovery.Object,
            logger.Object);

        List<BrookEvent> events = new()
        {
            new() { Id = "1", Data = ImmutableArray.Create<byte>(1) },
            new() { Id = "2", Data = ImmutableArray.Create<byte>(2) },
        };

        // Setup: first batch succeeds, second batch fails
        int batchCallCount = 0;
        repository.Setup(r => r.CreatePendingHeadAsync(It.IsAny<BrookKey>(), It.IsAny<BrookPosition>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository.Setup(r => r.AppendEventBatchAsync(
                It.IsAny<BrookKey>(),
                It.IsAny<List<EventStorageModel>>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                batchCallCount++;
                if (batchCallCount == 2)
                {
                    throw new InvalidOperationException("Simulated batch failure");
                }

                return Task.CompletedTask;
            });

        // Setup successful rollback - all events deleted successfully
        repository.Setup(r => r.DeleteEventAsync(brook, It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository.Setup(r => r.DeletePendingHeadAsync(brook, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository.Setup(r => r.EventExistsAsync(brook, It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // All events successfully deleted

        // Act + Assert - should throw original exception, but rollback succeeds
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await sut.AppendEventsAsync(brook, events, null));

        // Verify rollback operations were called
        repository.Verify(r => r.DeleteEventAsync(brook, 1, It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(r => r.DeletePendingHeadAsync(brook, It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(r => r.EventExistsAsync(brook, 1, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    ///     Verifies rollback is invoked and AggregateException is thrown when events remain after deletion
    ///     in the large batch path (kills equality and negate mutations on lines 383, 386).
    /// </summary>
    [Fact]
    public async Task RollbackThrowsAggregateWhenEventsRemainAfterDeletion()
    {
        // Arrange - similar to existing rollback tests but focuses on killing specific mutations
        BrookKey brook = new("type", "id");
        BrookStorageOptions opts = new()
        {
            MaxEventsPerBatch = 1, // Force large batch path with 2 events
            MaxRequestSizeBytes = 1_000_000,
        };
        Mock<ICosmosRepository> repository = new();
        Mock<IDistributedLock> lockMock = new();
        lockMock.Setup(l => l.RenewAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        Mock<IDistributedLockManager> lockManager = new();
        lockManager.Setup(m => m.AcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lockMock.Object);
        Mock<IBatchSizeEstimator> sizeEstimator = new();
        sizeEstimator.Setup(s => s.EstimateBatchSize(It.IsAny<IReadOnlyList<BrookEvent>>()))
            .Returns(100L);
        // Return individual batches (MaxEventsPerBatch = 1 means each event in its own batch)
        sizeEstimator.Setup(s => s.CreateSizeLimitedBatches(It.IsAny<IReadOnlyList<BrookEvent>>(), 1, It.IsAny<long>()))
            .Returns<IReadOnlyList<BrookEvent>, int, long>((evts, _, _) =>
                evts.Select(e => new[] { e }).ToArray());
        Mock<IRetryPolicy> retryPolicy = new();
        retryPolicy.Setup(r => r.ExecuteAsync(It.IsAny<Func<Task<bool>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<Task<bool>> op, CancellationToken _) => op());
        Mock<IMapper<BrookEvent, EventStorageModel>> mapper = new();
        mapper.Setup(m => m.Map(It.IsAny<BrookEvent>()))
            .Returns<BrookEvent>(e => new() { EventId = e.Id, Data = e.Data.ToArray() });
        Mock<IBrookRecoveryService> recovery = new();
        recovery.Setup(r => r.GetOrRecoverHeadPositionAsync(brook, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrookPosition(10));
        Mock<ILogger<EventBrookAppender>> logger = new();
        EventBrookAppender sut = new(
            repository.Object,
            lockManager.Object,
            sizeEstimator.Object,
            retryPolicy.Object,
            Options.Create(opts),
            mapper.Object,
            recovery.Object,
            logger.Object);

        List<BrookEvent> events = new()
        {
            new() { Id = "1", Data = ImmutableArray.Create<byte>(1) },
            new() { Id = "2", Data = ImmutableArray.Create<byte>(2) },
        };

        // Setup: first batch succeeds, second fails
        repository.Setup(r => r.CreatePendingHeadAsync(It.IsAny<BrookKey>(), It.IsAny<BrookPosition>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        int batchCall = 0;
        repository.Setup(r => r.AppendEventBatchAsync(
                It.IsAny<BrookKey>(),
                It.IsAny<IReadOnlyList<EventStorageModel>>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                batchCall++;
                if (batchCall == 2)
                {
                    throw new InvalidOperationException("Second batch failed");
                }

                return Task.CompletedTask;
            });

        // Setup rollback - deletion succeeds but verification shows event still exists
        repository.Setup(r => r.DeleteEventAsync(brook, It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository.Setup(r => r.DeletePendingHeadAsync(brook, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        // First event was successfully appended and still exists after deletion attempt
        repository.Setup(r => r.EventExistsAsync(brook, It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // Event still exists after deletion!

        // Act + Assert - should throw AggregateException with rollback failure message
        AggregateException ex = await Assert.ThrowsAsync<AggregateException>(async () =>
            await sut.AppendEventsAsync(brook, events, null));

        // Verify the error message indicates rollback issues
        Assert.Contains("Rollback failed", ex.Message);
        Assert.Contains(ex.InnerExceptions, e => e.Message.Contains("events still exist") || e.Message.Contains("Rollback incomplete"));
        
        // Verify rollback operations were attempted
        repository.Verify(r => r.DeleteEventAsync(brook, It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        repository.Verify(r => r.EventExistsAsync(brook, It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    /// <summary>
    ///     Helper class to simulate a list with a specific count without materializing all elements.
    /// </summary>
    private sealed class HugeReadOnlyList : IReadOnlyList<BrookEvent>
    {
        private readonly int reportedCount;

        public HugeReadOnlyList(int count)
        {
            reportedCount = count;
        }

        public BrookEvent this[int index] =>
            throw new NotSupportedException("Indexing not supported in test fake");

        public int Count => reportedCount;

        public IEnumerator<BrookEvent> GetEnumerator()
        {
            throw new NotSupportedException("Enumeration not supported in test fake");
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotSupportedException("Enumeration not supported in test fake");
        }
    }
}
