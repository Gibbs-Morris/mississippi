using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Common.Cosmos.Abstractions.Retry;
using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Cosmos.Batching;
using Mississippi.EventSourcing.Brooks.Cosmos.Brooks;
using Mississippi.EventSourcing.Brooks.Cosmos.Locking;
using Mississippi.EventSourcing.Brooks.Cosmos.Storage;

using Moq;


namespace Mississippi.EventSourcing.Brooks.Cosmos.L0Tests.Brooks;

/// <summary>
///     Unit tests for <see cref="EventBrookWriter" /> covering validation, single/large batch flows,
///     rollback behavior, and lease renewal.
/// </summary>
public sealed class EventBrookWriterTests
{
    /// <summary>
    ///     A minimal fake read-only list that reports a custom Count without allocating elements.
    /// </summary>
    private sealed class HugeReadOnlyList : IReadOnlyList<BrookEvent>
    {
        public HugeReadOnlyList(
            int countOverride
        ) =>
            Count = countOverride;

        public int Count { get; }

        public BrookEvent this[
            int index
        ] =>
            throw new NotSupportedException();

        public IEnumerator<BrookEvent> GetEnumerator() => throw new NotSupportedException();

        IEnumerator IEnumerable.GetEnumerator() => throw new NotSupportedException();
    }

    /// <summary>
    ///     Verifies <see cref="EventBrookWriter.AppendEventsAsync" /> creates a pending cursor entry before appending events
    ///     and commits the cursor afterwards.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task AppendEventsAsyncCreatesPendingCursorBeforeAppendAsync()
    {
        // Arrange
        BrookKey brook = new("type", "id");
        BrookPosition cursor = new(0);
        BrookEvent[] events = new[]
        {
            new BrookEvent
            {
                Id = "e1",
            },
        };
        Mock<ICosmosRepository> repository = new(MockBehavior.Strict);
        MockSequence seq = new();
        long final = cursor.Value + events.Length;
        repository.InSequence(seq)
            .Setup(r => r.CreatePendingCursorAsync(brook, cursor, final, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository.InSequence(seq)
            .Setup(r => r.AppendEventBatchAsync(
                brook,
                It.Is<IReadOnlyList<EventStorageModel>>(l => l.Count == 1),
                1,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository.InSequence(seq)
            .Setup(r => r.CommitCursorPositionAsync(brook, final, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        Mock<IDistributedLock> lockMock = new(MockBehavior.Strict);
        lockMock.Setup(l => l.RenewAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        lockMock.Setup(l => l.DisposeAsync()).Returns(default(ValueTask));
        Mock<IDistributedLockManager> lockManager = new(MockBehavior.Strict);
        lockManager
            .Setup(m => m.AcquireLockAsync(brook.ToString(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(lockMock.Object));
        Mock<IBatchSizeEstimator> sizeEstimator = new(MockBehavior.Strict);
        sizeEstimator.Setup(s => s.EstimateBatchSize(events)).Returns(10);
        Mock<IRetryPolicy> retryPolicy = new(MockBehavior.Strict);
        retryPolicy.Setup(p => p.ExecuteAsync(It.IsAny<Func<Task<bool>>>(), It.IsAny<CancellationToken>()))
            .Returns((
                Func<Task<bool>> op,
                CancellationToken _
            ) => op());
        Mock<IMapper<BrookEvent, EventStorageModel>> mapper = new(MockBehavior.Strict);
        mapper.Setup(m => m.Map(events[0]))
            .Returns(
                new EventStorageModel
                {
                    EventId = "e1",
                });
        Mock<IBrookRecoveryService> recovery = new(MockBehavior.Strict);
        recovery.Setup(r => r.GetOrRecoverCursorPositionAsync(brook, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cursor);
        Mock<ILogger<EventBrookWriter>> logger = new();
        EventBrookWriter sut = new(
            repository.Object,
            lockManager.Object,
            sizeEstimator.Object,
            retryPolicy.Object,
            Options.Create(
                new BrookStorageOptions
                {
                    MaxEventsPerBatch = 10,
                    MaxRequestSizeBytes = 1_000_000,
                }),
            mapper.Object,
            recovery.Object,
            logger.Object);

        // Act
        BrookPosition result = await sut.AppendEventsAsync(brook, events, null);

        // Assert (sequence ensures CreatePendingCursorAsync happened before AppendEventBatchAsync)
        Assert.Equal(1, result.Value);
        repository.VerifyAll();
    }

    /// <summary>
    ///     Verifies rollback on failure during large-batch append deletes created events and the pending cursor entry.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task AppendEventsAsyncRollsBackOnFailureAsync()
    {
        // Arrange
        BrookKey brook = new("type", "id");
        BrookPosition cursor = new(100);
        BrookEvent[] allEvents = new[]
        {
            new BrookEvent
            {
                Id = "e1",
            },
            new BrookEvent
            {
                Id = "e2",
            },
            new BrookEvent
            {
                Id = "e3",
            },
            new BrookEvent
            {
                Id = "e4",
            },
        };
        Mock<ICosmosRepository> repository = new(MockBehavior.Strict);
        Mock<IDistributedLock> lockMock = new(MockBehavior.Strict);
        lockMock.Setup(l => l.DisposeAsync()).Returns(default(ValueTask));
        Mock<IDistributedLockManager> lockManager = new(MockBehavior.Strict);
        lockManager
            .Setup(m => m.AcquireLockAsync(brook.ToString(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(lockMock.Object));
        Mock<IBatchSizeEstimator> sizeEstimator = new(MockBehavior.Strict);
        sizeEstimator.Setup(s => s.EstimateBatchSize(It.IsAny<IReadOnlyList<BrookEvent>>())).Returns(1_000);

        // two batches of two events
        BrookEvent[] b1 = new[] { allEvents[0], allEvents[1] };
        BrookEvent[] b2 = new[] { allEvents[2], allEvents[3] };
        sizeEstimator.Setup(s => s.CreateSizeLimitedBatches(allEvents, 2, It.IsAny<long>())).Returns(new[] { b1, b2 });
        Mock<IRetryPolicy> retryPolicy = new(MockBehavior.Strict);
        retryPolicy.Setup(p => p.ExecuteAsync(It.IsAny<Func<Task<bool>>>(), It.IsAny<CancellationToken>()))
            .Returns((
                Func<Task<bool>> op,
                CancellationToken _
            ) => op());
        retryPolicy.Setup(p => p.ExecuteAsync(It.IsAny<Func<Task<bool>>>(), It.IsAny<CancellationToken>()))
            .Returns((
                Func<Task<bool>> op,
                CancellationToken _
            ) => op());
        Mock<IMapper<BrookEvent, EventStorageModel>> mapper = new(MockBehavior.Strict);
        mapper.Setup(m => m.Map(allEvents[0]))
            .Returns(
                new EventStorageModel
                {
                    EventId = "e1",
                });
        mapper.Setup(m => m.Map(allEvents[1]))
            .Returns(
                new EventStorageModel
                {
                    EventId = "e2",
                });
        mapper.Setup(m => m.Map(allEvents[2]))
            .Returns(
                new EventStorageModel
                {
                    EventId = "e3",
                });
        mapper.Setup(m => m.Map(allEvents[3]))
            .Returns(
                new EventStorageModel
                {
                    EventId = "e4",
                });
        Mock<IBrookRecoveryService> recovery = new(MockBehavior.Strict);
        recovery.Setup(r => r.GetOrRecoverCursorPositionAsync(brook, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(cursor));
        long final = cursor.Value + allEvents.Length;
        repository.Setup(r => r.CreatePendingCursorAsync(brook, cursor, final, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // First batch succeeds
        repository.Setup(r => r.AppendEventBatchAsync(
                brook,
                It.Is<IReadOnlyList<EventStorageModel>>(l => l.Count == 2),
                cursor.Value + 1,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Second batch fails -> triggers rollback
        repository.Setup(r => r.AppendEventBatchAsync(
                brook,
                It.Is<IReadOnlyList<EventStorageModel>>(l => l.Count == 2),
                cursor.Value + 3,
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromException(new InvalidOperationException("batch failure")));

        // Rollback expectations for positions 101 and 102
        repository.Setup(r => r.DeleteEventAsync(brook, 101, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository.Setup(r => r.DeleteEventAsync(brook, 102, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository.Setup(r => r.DeletePendingCursorAsync(brook, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository.Setup(r => r.EventExistsAsync(brook, 101, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(false));
        repository.Setup(r => r.EventExistsAsync(brook, 102, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(false));
        Mock<ILogger<EventBrookWriter>> logger = new();
        EventBrookWriter sut = new(
            repository.Object,
            lockManager.Object,
            sizeEstimator.Object,
            retryPolicy.Object,
            Options.Create(
                new BrookStorageOptions
                {
                    MaxEventsPerBatch = 2,
                    MaxRequestSizeBytes = 1_000_000,
                }),
            mapper.Object,
            recovery.Object,
            logger.Object);

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await sut.AppendEventsAsync(brook, allEvents, null));
        repository.VerifyAll();
        sizeEstimator.VerifyAll();
        mapper.VerifyAll();
        recovery.VerifyAll();
        lockManager.VerifyAll();
        lockMock.VerifyAll();
        retryPolicy.VerifyAll();
    }

    /// <summary>
    ///     Verifies <see cref="EventBrookWriter.AppendEventsAsync" /> throws when events is null or empty.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task AppendEventsAsyncThrowsOnNullOrEmptyEventsAsync()
    {
        // Arrange
        Mock<ICosmosRepository> repository = new(MockBehavior.Strict);
        Mock<IDistributedLockManager> lockManager = new(MockBehavior.Strict);
        Mock<IBatchSizeEstimator> sizeEstimator = new(MockBehavior.Strict);
        Mock<IRetryPolicy> retryPolicy = new(MockBehavior.Strict);
        retryPolicy.Setup(p => p.ExecuteAsync(It.IsAny<Func<Task<bool>>>(), It.IsAny<CancellationToken>()))
            .Returns((
                Func<Task<bool>> op,
                CancellationToken _
            ) => op());
        retryPolicy.Setup(p => p.ExecuteAsync(It.IsAny<Func<Task<bool>>>(), It.IsAny<CancellationToken>()))
            .Returns((
                Func<Task<bool>> op,
                CancellationToken _
            ) => op());
        Mock<IMapper<BrookEvent, EventStorageModel>> mapper = new(MockBehavior.Strict);
        Mock<IBrookRecoveryService> recovery = new(MockBehavior.Strict);
        Mock<ILogger<EventBrookWriter>> logger = new();
        BrookStorageOptions options = new();
        EventBrookWriter sut = new(
            repository.Object,
            lockManager.Object,
            sizeEstimator.Object,
            retryPolicy.Object,
            Options.Create(options),
            mapper.Object,
            recovery.Object,
            logger.Object);
        BrookKey brook = new("type", "id");

        // Act + Assert
        await Assert.ThrowsAsync<ArgumentException>(async () => await sut.AppendEventsAsync(brook, null!, null));
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await sut.AppendEventsAsync(brook, Array.Empty<BrookEvent>(), null));
    }

    /// <summary>
    ///     Verifies overflow in position calculation is detected and throws.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task AppendEventsAsyncThrowsOnPositionOverflowAsync()
    {
        // Arrange
        BrookKey brook = new("type", "id");
        BrookPosition cursor = new(long.MaxValue - 5);
        BrookEvent[] events = new[]
        {
            new BrookEvent
            {
                Id = "e1",
            },
            new BrookEvent
            {
                Id = "e2",
            },
            new BrookEvent
            {
                Id = "e3",
            },
            new BrookEvent
            {
                Id = "e4",
            },
            new BrookEvent
            {
                Id = "e5",
            },
            new BrookEvent
            {
                Id = "e6",
            },
            new BrookEvent
            {
                Id = "e7",
            },
            new BrookEvent
            {
                Id = "e8",
            },
            new BrookEvent
            {
                Id = "e9",
            },
            new BrookEvent
            {
                Id = "e10",
            },
        };
        Mock<ICosmosRepository> repository = new(MockBehavior.Strict);
        Mock<IDistributedLock> lockMock = new(MockBehavior.Strict);
        lockMock.Setup(l => l.DisposeAsync()).Returns(default(ValueTask));
        Mock<IDistributedLockManager> lockManager = new(MockBehavior.Strict);
        lockManager
            .Setup(m => m.AcquireLockAsync(brook.ToString(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(lockMock.Object));
        Mock<IBatchSizeEstimator> sizeEstimator = new(MockBehavior.Strict);
        Mock<IRetryPolicy> retryPolicy = new(MockBehavior.Strict);
        Mock<IMapper<BrookEvent, EventStorageModel>> mapper = new(MockBehavior.Strict);
        Mock<IBrookRecoveryService> recovery = new(MockBehavior.Strict);
        recovery.Setup(r => r.GetOrRecoverCursorPositionAsync(brook, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(cursor));
        Mock<ILogger<EventBrookWriter>> logger = new();
        EventBrookWriter sut = new(
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
    ///     Verifies <see cref="EventBrookWriter.AppendEventsAsync" /> throws when events.Count exceeds hard safety limit.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task AppendEventsAsyncThrowsOnTooManyEventsAsync()
    {
        // Arrange a fake list reporting an enormous Count to trigger early guard.
        IReadOnlyList<BrookEvent> hugeList = new HugeReadOnlyList(int.MaxValue);
        Mock<ICosmosRepository> repository = new(MockBehavior.Strict);
        Mock<IDistributedLockManager> lockManager = new(MockBehavior.Strict);
        Mock<IBatchSizeEstimator> sizeEstimator = new(MockBehavior.Strict);
        Mock<IRetryPolicy> retryPolicy = new(MockBehavior.Strict);
        Mock<IMapper<BrookEvent, EventStorageModel>> mapper = new(MockBehavior.Strict);
        Mock<IBrookRecoveryService> recovery = new(MockBehavior.Strict);
        Mock<ILogger<EventBrookWriter>> logger = new();
        EventBrookWriter sut = new(
            repository.Object,
            lockManager.Object,
            sizeEstimator.Object,
            retryPolicy.Object,
            Options.Create(new BrookStorageOptions()),
            mapper.Object,
            recovery.Object,
            logger.Object);

        // Act + Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await sut.AppendEventsAsync(new("t", "1"), hugeList, null));
    }

    /// <summary>
    ///     Verifies optimistic concurrency check throws when expected version mismatches the current cursor position.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task AppendEventsAsyncThrowsOnVersionMismatchAsync()
    {
        // Arrange
        BrookKey brook = new("type", "id");
        BrookEvent[] events = new[]
        {
            new BrookEvent
            {
                Id = "e1",
                EventType = "T",
            },
        };
        Mock<ICosmosRepository> repository = new(MockBehavior.Strict);
        Mock<IDistributedLock> lockMock = new(MockBehavior.Strict);
        lockMock.Setup(l => l.DisposeAsync()).Returns(default(ValueTask));
        Mock<IDistributedLockManager> lockManager = new(MockBehavior.Strict);
        lockManager
            .Setup(m => m.AcquireLockAsync(brook.ToString(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(lockMock.Object));
        Mock<IBatchSizeEstimator> sizeEstimator = new(MockBehavior.Strict);
        Mock<IRetryPolicy> retryPolicy = new(MockBehavior.Strict);
        Mock<IMapper<BrookEvent, EventStorageModel>> mapper = new(MockBehavior.Strict);
        Mock<IBrookRecoveryService> recovery = new(MockBehavior.Strict);
        recovery.Setup(r => r.GetOrRecoverCursorPositionAsync(brook, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new BrookPosition(5)));
        Mock<ILogger<EventBrookWriter>> logger = new();
        EventBrookWriter sut = new(
            repository.Object,
            lockManager.Object,
            sizeEstimator.Object,
            retryPolicy.Object,
            Options.Create(new BrookStorageOptions()),
            mapper.Object,
            recovery.Object,
            logger.Object);

        // Act + Assert
        await Assert.ThrowsAsync<OptimisticConcurrencyException>(async () =>
            await sut.AppendEventsAsync(brook, events, new BrookPosition(3)));
    }

    /// <summary>
    ///     Verifies the large-batch path is selected and appends multiple batches with correct positions.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task AppendEventsAsyncUsesLargeBatchPathAsync()
    {
        // Arrange
        BrookKey brook = new("type", "id");
        BrookPosition cursor = new(10);
        BrookEvent[] allEvents = new[]
        {
            new BrookEvent
            {
                Id = "e1",
            },
            new BrookEvent
            {
                Id = "e2",
            },
            new BrookEvent
            {
                Id = "e3",
            },
            new BrookEvent
            {
                Id = "e4",
            },
            new BrookEvent
            {
                Id = "e5",
            },
            new BrookEvent
            {
                Id = "e6",
            },
        };
        Mock<ICosmosRepository> repository = new(MockBehavior.Strict);
        Mock<IDistributedLock> lockMock = new(MockBehavior.Strict);
        lockMock.Setup(l => l.DisposeAsync()).Returns(default(ValueTask));
        Mock<IDistributedLockManager> lockManager = new(MockBehavior.Strict);
        lockManager
            .Setup(m => m.AcquireLockAsync(brook.ToString(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(lockMock.Object));
        Mock<IBatchSizeEstimator> sizeEstimator = new(MockBehavior.Strict);
        sizeEstimator.Setup(s => s.EstimateBatchSize(It.IsAny<IReadOnlyList<BrookEvent>>())).Returns(1000);

        // Create three batches of two events each
        BrookEvent[] b1 = new[] { allEvents[0], allEvents[1] };
        BrookEvent[] b2 = new[] { allEvents[2], allEvents[3] };
        BrookEvent[] b3 = new[] { allEvents[4], allEvents[5] };
        sizeEstimator.Setup(s => s.CreateSizeLimitedBatches(allEvents, 2, It.IsAny<long>()))
            .Returns(new[] { b1, b2, b3 });
        Mock<IRetryPolicy> retryPolicy = new(MockBehavior.Strict);
        retryPolicy.Setup(p => p.ExecuteAsync(It.IsAny<Func<Task<bool>>>(), It.IsAny<CancellationToken>()))
            .Returns((
                Func<Task<bool>> op,
                CancellationToken _
            ) => op());
        Mock<IMapper<BrookEvent, EventStorageModel>> mapper = new(MockBehavior.Strict);
        mapper.Setup(m => m.Map(allEvents[0]))
            .Returns(
                new EventStorageModel
                {
                    EventId = "e1",
                });
        mapper.Setup(m => m.Map(allEvents[1]))
            .Returns(
                new EventStorageModel
                {
                    EventId = "e2",
                });
        mapper.Setup(m => m.Map(allEvents[2]))
            .Returns(
                new EventStorageModel
                {
                    EventId = "e3",
                });
        mapper.Setup(m => m.Map(allEvents[3]))
            .Returns(
                new EventStorageModel
                {
                    EventId = "e4",
                });
        mapper.Setup(m => m.Map(allEvents[4]))
            .Returns(
                new EventStorageModel
                {
                    EventId = "e5",
                });
        mapper.Setup(m => m.Map(allEvents[5]))
            .Returns(
                new EventStorageModel
                {
                    EventId = "e6",
                });
        Mock<IBrookRecoveryService> recovery = new(MockBehavior.Strict);
        recovery.Setup(r => r.GetOrRecoverCursorPositionAsync(brook, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(cursor));
        long final = cursor.Value + allEvents.Length;
        repository.Setup(r => r.CreatePendingCursorAsync(brook, cursor, final, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository.Setup(r => r.AppendEventBatchAsync(
                brook,
                It.Is<IReadOnlyList<EventStorageModel>>(l => l.Count == 2),
                cursor.Value + 1,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository.Setup(r => r.AppendEventBatchAsync(
                brook,
                It.Is<IReadOnlyList<EventStorageModel>>(l => l.Count == 2),
                cursor.Value + 3,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository.Setup(r => r.AppendEventBatchAsync(
                brook,
                It.Is<IReadOnlyList<EventStorageModel>>(l => l.Count == 2),
                cursor.Value + 5,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository.Setup(r => r.CommitCursorPositionAsync(brook, final, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        Mock<ILogger<EventBrookWriter>> logger = new();
        EventBrookWriter sut = new(
            repository.Object,
            lockManager.Object,
            sizeEstimator.Object,
            retryPolicy.Object,
            Options.Create(
                new BrookStorageOptions
                {
                    MaxEventsPerBatch = 2,
                    MaxRequestSizeBytes = 1_000_000,
                }),
            mapper.Object,
            recovery.Object,
            logger.Object);

        // Act
        BrookPosition result = await sut.AppendEventsAsync(brook, allEvents, null);

        // Assert
        Assert.Equal(final, result.Value);
        repository.VerifyAll();
        sizeEstimator.VerifyAll();
        mapper.VerifyAll();
        recovery.VerifyAll();
        lockManager.VerifyAll();
        lockMock.VerifyAll();
    }

    /// <summary>
    ///     Verifies the single-batch path is used under thresholds and commits the cursor position.
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task AppendEventsAsyncUsesSingleBatchPathAsync()
    {
        // Arrange
        BrookKey brook = new("type", "id");
        BrookPosition cursor = new(2);
        BrookEvent[] events = new[]
        {
            new BrookEvent
            {
                Id = "e1",
                EventType = "T1",
            },
            new BrookEvent
            {
                Id = "e2",
                EventType = "T2",
            },
            new BrookEvent
            {
                Id = "e3",
                EventType = "T3",
            },
        };
        Mock<ICosmosRepository> repository = new(MockBehavior.Strict);
        Mock<IDistributedLock> lockMock = new(MockBehavior.Strict);
        lockMock.Setup(l => l.RenewAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        lockMock.Setup(l => l.DisposeAsync()).Returns(default(ValueTask));
        Mock<IDistributedLockManager> lockManager = new(MockBehavior.Strict);
        lockManager
            .Setup(m => m.AcquireLockAsync(brook.ToString(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(lockMock.Object));
        Mock<IBatchSizeEstimator> sizeEstimator = new(MockBehavior.Strict);
        sizeEstimator.Setup(s => s.EstimateBatchSize(events)).Returns(100);
        Mock<IRetryPolicy> retryPolicy = new(MockBehavior.Strict);
        retryPolicy.Setup(p => p.ExecuteAsync(It.IsAny<Func<Task<bool>>>(), It.IsAny<CancellationToken>()))
            .Returns((
                Func<Task<bool>> op,
                CancellationToken _
            ) => op());
        Mock<IMapper<BrookEvent, EventStorageModel>> mapper = new(MockBehavior.Strict);
        mapper.Setup(m => m.Map(It.Is<BrookEvent>(e => e.Id == "e1")))
            .Returns(
                new EventStorageModel
                {
                    EventId = "e1",
                });
        mapper.Setup(m => m.Map(It.Is<BrookEvent>(e => e.Id == "e2")))
            .Returns(
                new EventStorageModel
                {
                    EventId = "e2",
                });
        mapper.Setup(m => m.Map(It.Is<BrookEvent>(e => e.Id == "e3")))
            .Returns(
                new EventStorageModel
                {
                    EventId = "e3",
                });
        Mock<IBrookRecoveryService> recovery = new(MockBehavior.Strict);
        recovery.Setup(r => r.GetOrRecoverCursorPositionAsync(brook, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(cursor));
        long final = cursor.Value + events.Length;
        repository.Setup(r => r.CreatePendingCursorAsync(brook, cursor, final, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository.Setup(r => r.AppendEventBatchAsync(
                brook,
                It.Is<IReadOnlyList<EventStorageModel>>(lst => lst.Count == 3),
                cursor.Value + 1,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository.Setup(r => r.CommitCursorPositionAsync(brook, final, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        Mock<ILogger<EventBrookWriter>> logger = new();
        EventBrookWriter sut = new(
            repository.Object,
            lockManager.Object,
            sizeEstimator.Object,
            retryPolicy.Object,
            Options.Create(
                new BrookStorageOptions
                {
                    MaxEventsPerBatch = 100,
                    MaxRequestSizeBytes = 1_000_000,
                }),
            mapper.Object,
            recovery.Object,
            logger.Object);

        // Act
        BrookPosition result = await sut.AppendEventsAsync(brook, events, null);

        // Assert
        Assert.Equal(final, result.Value);
        repository.VerifyAll();
        sizeEstimator.VerifyAll();
        retryPolicy.VerifyAll();
        mapper.VerifyAll();
        recovery.VerifyAll();
        lockManager.VerifyAll();
        lockMock.VerifyAll();
    }

    /// <summary>
    ///     Verifies large-batch flow renews the distributed lock according to threshold (every 5th batch).
    /// </summary>
    /// <returns>A task representing the asynchronous test execution.</returns>
    [Fact]
    public async Task AppendLargeBatchAsyncRenewsLockPerThresholdAsync()
    {
        // Arrange 6 batches to trigger renewal at batchIndex 5 (0-based), since 5 % 5 == 0
        BrookKey brook = new("type", "id");
        BrookPosition cursor = new(0);
        List<BrookEvent> allEvents = new();
        for (int i = 1; i <= 12; i++)
        {
            allEvents.Add(
                new()
                {
                    Id = $"e{i}",
                });
        }

        Mock<ICosmosRepository> repository = new(MockBehavior.Strict);
        Mock<IDistributedLock> lockMock = new(MockBehavior.Strict);
        lockMock.Setup(l => l.RenewAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        lockMock.Setup(l => l.DisposeAsync()).Returns(default(ValueTask));
        Mock<IDistributedLockManager> lockManager = new(MockBehavior.Strict);
        lockManager
            .Setup(m => m.AcquireLockAsync(brook.ToString(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lockMock.Object);
        Mock<IBatchSizeEstimator> sizeEstimator = new(MockBehavior.Strict);
        sizeEstimator.Setup(s => s.EstimateBatchSize(It.IsAny<IReadOnlyList<BrookEvent>>())).Returns(10_000);

        // 6 batches of 2 events each
        List<IReadOnlyList<BrookEvent>> batches = new();
        for (int i = 0; i < 6; i++)
        {
            batches.Add(new[] { allEvents[i * 2], allEvents[(i * 2) + 1] });
        }

        sizeEstimator.Setup(s => s.CreateSizeLimitedBatches(allEvents, 2, It.IsAny<long>())).Returns(batches);
        Mock<IRetryPolicy> retryPolicy = new(MockBehavior.Strict);
        retryPolicy.Setup(p => p.ExecuteAsync(It.IsAny<Func<Task<bool>>>(), It.IsAny<CancellationToken>()))
            .Returns((
                Func<Task<bool>> op,
                CancellationToken _
            ) => op());
        Mock<IMapper<BrookEvent, EventStorageModel>> mapper = new(MockBehavior.Strict);
        foreach (BrookEvent ev in allEvents)
        {
            mapper.Setup(m => m.Map(ev))
                .Returns(
                    new EventStorageModel
                    {
                        EventId = ev.Id,
                    });
        }

        Mock<IBrookRecoveryService> recovery = new(MockBehavior.Strict);
        recovery.Setup(r => r.GetOrRecoverCursorPositionAsync(brook, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(cursor));
        long final = cursor.Value + allEvents.Count;
        repository.Setup(r => r.CreatePendingCursorAsync(brook, cursor, final, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Expect 6 appends starting at positions 1,3,5,7,9,11
        for (int i = 0; i < 6; i++)
        {
            long start = cursor.Value + (i * 2) + 1;
            repository.Setup(r => r.AppendEventBatchAsync(
                    brook,
                    It.Is<IReadOnlyList<EventStorageModel>>(l => l.Count == 2),
                    start,
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        repository.Setup(r => r.CommitCursorPositionAsync(brook, final, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        Mock<ILogger<EventBrookWriter>> logger = new();
        EventBrookWriter sut = new(
            repository.Object,
            lockManager.Object,
            sizeEstimator.Object,
            retryPolicy.Object,
            Options.Create(
                new BrookStorageOptions
                {
                    MaxEventsPerBatch = 2,
                    MaxRequestSizeBytes = 1_000_000,
                    LeaseRenewalThresholdSeconds =
                        1000, // ensure time-based condition does not trigger, rely on (batchIndex % 5 == 0)
                }),
            mapper.Object,
            recovery.Object,
            logger.Object);

        // Act
        BrookPosition result = await sut.AppendEventsAsync(brook, allEvents, null);

        // Assert
        Assert.Equal(final, result.Value);

        // Renew should be called at least once (on batch index 5)
        lockMock.Verify(l => l.RenewAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        repository.VerifyAll();
        sizeEstimator.VerifyAll();
        mapper.VerifyAll();
        recovery.VerifyAll();
        lockManager.VerifyAll();
    }
}