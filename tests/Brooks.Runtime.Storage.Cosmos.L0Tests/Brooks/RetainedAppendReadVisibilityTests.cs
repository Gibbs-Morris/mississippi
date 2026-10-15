using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

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
///     Keeps failed-append evidence out of ordinary public range reads.
/// </summary>
public sealed class RetainedAppendReadVisibilityTests
{
    private const int AppendFailure = 0;

    private const int CallerCancellation = 2;

    private const int CommitFailure = 1;

    private const int SuccessfulAppend = 3;

    private static async IAsyncEnumerable<EventStorageModel> QueryAsync(
        IReadOnlyDictionary<long, EventStorageModel> documents,
        BrookRangeKey range,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        foreach (KeyValuePair<long, EventStorageModel> item in documents.OrderBy(item => item.Key))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if ((item.Key >= range.Start.Value) && (item.Key <= range.End.Value))
            {
                yield return item.Value;
                await Task.Yield();
            }
        }
    }

    private sealed class DirectRetryPolicy : IRetryPolicy
    {
        public Task<T> ExecuteAsync<T>(
            Func<Task<T>> operation,
            CancellationToken cancellationToken = default
        ) =>
            operation();
    }

    /// <summary>
    ///     Reads the committed prefix while keeping failed append documents available for recovery.
    /// </summary>
    /// <param name="large">Whether append uses separate batches.</param>
    /// <param name="failureStage">The append, commit, cancellation or successful control path.</param>
    /// <returns>The asynchronous test.</returns>
    [Theory]
    [InlineData(false, AppendFailure)]
    [InlineData(true, AppendFailure)]
    [InlineData(false, CommitFailure)]
    [InlineData(true, CommitFailure)]
    [InlineData(false, CallerCancellation)]
    [InlineData(true, CallerCancellation)]
    [InlineData(false, SuccessfulAppend)]
    [InlineData(true, SuccessfulAppend)]
    public async Task ExplicitRangeReadsOnlyCommittedEventsAfterAppendAsync(
        bool large,
        int failureStage
    )
    {
        using CancellationTokenSource caller =
            CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        caller.CancelAfter(TimeSpan.FromSeconds(30));
        BrookKey key = new("test", "retained-read");
        BrookPosition committed = new(10);
        CursorStorageModel? pending = null;
        Dictionary<long, EventStorageModel> documents = new()
        {
            [10] = new()
            {
                EventId = "committed-10",
            },
        };
        IOException expectedFailure = new("Controlled append storage failure.");

        async Task AppendBatchAsync(
            BrookKey brookId,
            IReadOnlyList<EventStorageModel> events,
            long start,
            Action beforeDispatch,
            CancellationToken token
        )
        {
            Assert.Equal(key, brookId);
            for (int index = 0; index < events.Count; index++)
            {
                beforeDispatch();
                documents[start + index] = events[index];
                if (failureStage == AppendFailure)
                {
                    throw expectedFailure;
                }

                if (failureStage == CallerCancellation)
                {
                    await caller.CancelAsync();
                    token.ThrowIfCancellationRequested();
                }
            }
        }

        Mock<ICosmosRepository> repository = new(MockBehavior.Strict);
        repository.Setup(r => r.GetCursorDocumentAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new()
            {
                Position = committed,
            });
        repository.Setup(r => r.CreatePendingCursorAsync(
                key,
                new(10),
                12,
                It.IsAny<Action>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => pending = new()
            {
                OriginalPosition = new(10),
                Position = new(12),
                ETag = "pending-attempt",
            })
            .ReturnsAsync("pending-attempt");
        repository.Setup(r => r.AppendEventBatchAsync(
                key,
                It.IsAny<IReadOnlyList<EventStorageModel>>(),
                It.IsAny<long>(),
                It.IsAny<Action>(),
                It.IsAny<CancellationToken>()))
            .Returns<BrookKey, IReadOnlyList<EventStorageModel>, long, Action, CancellationToken>(AppendBatchAsync);
        repository.Setup(r => r.CommitCursorPositionAsync(
                key,
                12,
                "pending-attempt",
                It.IsAny<Action>(),
                It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                if (failureStage == CommitFailure)
                {
                    throw expectedFailure;
                }

                committed = new(12);
                pending = null;
                return Task.CompletedTask;
            });
        repository
            .Setup(r => r.QueryEventsAsync(It.IsAny<BrookRangeKey>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns((BrookRangeKey range, int _, CancellationToken token) => QueryAsync(documents, range, token));
        Mock<IDistributedLock> lease = new(MockBehavior.Strict);
        lease.SetupGet(l => l.LockId).Returns("append-lease");
        lease.Setup(l => l.RenewAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        lease.Setup(l => l.RenewAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        lease.Setup(l => l.DisposeAsync()).Returns(ValueTask.CompletedTask);
        Mock<IDistributedLockManager> locks = new(MockBehavior.Strict);
        locks.Setup(l => l.AcquireLockAsync(key.ToString(), TimeSpan.FromSeconds(60), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lease.Object);
        Mock<IBrookRecoveryService> recovery = new(MockBehavior.Strict);
        recovery.Setup(r => r.GetOrRecoverCursorPositionAsync(key, It.IsAny<Action>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BrookPosition(10));
        DirectRetryPolicy retry = new();
        Mock<IMapper<BrookEvent, EventStorageModel>> toStorage = new();
        toStorage.Setup(m => m.Map(It.IsAny<BrookEvent>()))
            .Returns((BrookEvent item) => new()
            {
                EventId = item.Id,
            });
        Mock<IMapper<EventStorageModel, BrookEvent>> toEvent = new();
        toEvent.Setup(m => m.Map(It.IsAny<EventStorageModel>()))
            .Returns((EventStorageModel item) => new()
            {
                Id = item.EventId,
            });
        IOptions<BrookStorageOptions> options = Options.Create(
            new BrookStorageOptions
            {
                MaxEventsPerBatch = large ? 1 : 100,
            });
        EventBrookWriter writer = new(
            repository.Object,
            locks.Object,
            new BatchSizeEstimator(),
            retry,
            options,
            toStorage.Object,
            recovery.Object,
            NullLogger<EventBrookWriter>.Instance,
            new FakeTimeProvider());
        EventBrookReader reader = new(
            repository.Object,
            retry,
            options,
            toEvent.Object,
            NullLogger<EventBrookReader>.Instance);
        BrookStorageProvider provider = new(recovery.Object, reader, writer);
        BrookEvent[] attempted =
        [
            new()
            {
                Id = "attempt-11",
            },
            new()
            {
                Id = "attempt-12",
            },
        ];
        if (failureStage == SuccessfulAppend)
        {
            Assert.Equal(new(12), await provider.AppendEventsAsync(key, attempted, new(10), caller.Token));
        }
        else if (failureStage == CallerCancellation)
        {
            OperationCanceledException failure = await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await provider.AppendEventsAsync(key, attempted, new(10), caller.Token));
            Assert.Equal(caller.Token, failure.CancellationToken);
        }
        else
        {
            IOException failure = await Assert.ThrowsAsync<IOException>(async () =>
                await provider.AppendEventsAsync(key, attempted, new(10), caller.Token));
            Assert.Same(expectedFailure, failure);
        }

        int retainedCount = documents.Count;
        List<string> visible = new();
        await foreach (BrookEvent item in provider.ReadEventsAsync(
                           BrookRangeKey.FromBrookCompositeKey(key, 10, 5),
                           TestContext.Current.CancellationToken))
        {
            visible.Add(item.Id);
        }

        if (failureStage == SuccessfulAppend)
        {
            Assert.Equal(3, visible.Count);
            Assert.Equal("committed-10", visible[0]);
            Assert.Equal("attempt-11", visible[1]);
            Assert.Equal("attempt-12", visible[2]);
            Assert.Null(pending);
        }
        else
        {
            Assert.Equal("committed-10", Assert.Single(visible));
            Assert.Equal(new(10), committed);
            Assert.NotNull(pending);
            Assert.Equal(new BrookPosition(10), pending.OriginalPosition);
            Assert.Equal(new(12), pending.Position);
            Assert.True(retainedCount >= 2);
        }

        Assert.Equal(retainedCount, documents.Count);
        repository.Verify(
            r => r.DeleteEventAsync(It.IsAny<BrookKey>(), It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
        repository.Verify(
            r => r.DeletePendingCursorAsync(It.IsAny<BrookKey>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        lease.Verify(l => l.DisposeAsync(), Times.Once);
    }

    /// <summary>
    ///     Hides retained rows when a new brook has no committed cursor.
    /// </summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task NoCommittedCursorHidesRetainedNewBrookRowsAsync()
    {
        BrookKey key = new("test", "uncommitted-new-brook");
        Dictionary<long, EventStorageModel> documents = new()
        {
            [0] = new()
            {
                EventId = "retained-0",
            },
        };
        Mock<ICosmosRepository> repository = new(MockBehavior.Strict);
        repository.Setup(r => r.GetCursorDocumentAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CursorStorageModel?)null);
        repository
            .Setup(r => r.QueryEventsAsync(It.IsAny<BrookRangeKey>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns((BrookRangeKey range, int _, CancellationToken token) => QueryAsync(documents, range, token));
        Mock<IMapper<EventStorageModel, BrookEvent>> mapper = new();
        mapper.Setup(m => m.Map(It.IsAny<EventStorageModel>()))
            .Returns((EventStorageModel item) => new()
            {
                Id = item.EventId,
            });
        DirectRetryPolicy retry = new();
        EventBrookReader reader = new(
            repository.Object,
            retry,
            Options.Create(new BrookStorageOptions()),
            mapper.Object,
            NullLogger<EventBrookReader>.Instance);
        List<BrookEvent> visible = new();
        await foreach (BrookEvent item in reader.ReadEventsAsync(
                           BrookRangeKey.FromBrookCompositeKey(key, 0, 2),
                           TestContext.Current.CancellationToken))
        {
            visible.Add(item);
        }

        Assert.Empty(visible);
        Assert.Single(documents);
    }

    /// <summary>
    ///     Avoids reading retained rows for an empty request or a range wholly after the committed prefix.
    /// </summary>
    /// <param name="start">The requested start position.</param>
    /// <param name="count">The requested event count.</param>
    /// <returns>The asynchronous test.</returns>
    [Theory]
    [InlineData(0L, 0L)]
    [InlineData(11L, 2L)]
    public async Task RangesWithoutCommittedEventsLeaveRetainedRowsUntouchedAsync(
        long start,
        long count
    )
    {
        BrookKey key = new("test", "range-after-commit");
        Dictionary<long, EventStorageModel> documents = new()
        {
            [11] = new()
            {
                EventId = "retained-11",
            },
        };
        Mock<ICosmosRepository> repository = new(MockBehavior.Strict);
        repository.Setup(r => r.GetCursorDocumentAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new CursorStorageModel
                {
                    Position = new(10),
                });
        repository
            .Setup(r => r.QueryEventsAsync(It.IsAny<BrookRangeKey>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns((BrookRangeKey range, int _, CancellationToken token) => QueryAsync(documents, range, token));
        Mock<IMapper<EventStorageModel, BrookEvent>> mapper = new();
        mapper.Setup(m => m.Map(It.IsAny<EventStorageModel>()))
            .Returns((EventStorageModel item) => new()
            {
                Id = item.EventId,
            });
        DirectRetryPolicy retry = new();
        EventBrookReader reader = new(
            repository.Object,
            retry,
            Options.Create(new BrookStorageOptions()),
            mapper.Object,
            NullLogger<EventBrookReader>.Instance);
        List<BrookEvent> visible = new();
        await foreach (BrookEvent item in reader.ReadEventsAsync(
                           BrookRangeKey.FromBrookCompositeKey(key, start, count),
                           TestContext.Current.CancellationToken))
        {
            visible.Add(item);
        }

        Assert.Empty(visible);
        Assert.Single(documents);
        repository.Verify(
            r => r.QueryEventsAsync(It.IsAny<BrookRangeKey>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}