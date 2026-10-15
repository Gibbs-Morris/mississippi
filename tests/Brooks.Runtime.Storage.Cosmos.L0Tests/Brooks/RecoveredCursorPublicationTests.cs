using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Abstractions.Streaming;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Batching;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Brooks;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Locking;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Storage;
using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Common.Runtime.Storage.Abstractions.Retry;

using Moq;

using Range = Moq.Range;


namespace Mississippi.Brooks.Runtime.Storage.Cosmos.L0Tests.Brooks;

/// <summary>
///     Publishes a recovered committed prefix before a new append can fail.
/// </summary>
public sealed class RecoveredCursorPublicationTests
{
    private const int CallerCancellation = 3;

    private const int CursorCommitFailure = 2;

    private const int EventAppendFailure = 1;

    private const int ExpectedVersionFailure = 0;

    private const int StandaloneReaderControl = 4;

    private sealed class DirectRetryPolicy : IRetryPolicy
    {
        public Task<T> ExecuteAsync<T>(
            Func<Task<T>> operation,
            CancellationToken cancellationToken = default
        ) =>
            operation();
    }

    /// <summary>
    ///     Keeps the recovered prefix observable when the following command does not commit.
    /// </summary>
    /// <param name="large">Whether the following command uses separate event batches.</param>
    /// <param name="failureStage">The new append failure or standalone reader control.</param>
    /// <returns>The asynchronous test.</returns>
    [Theory]
    [InlineData(false, ExpectedVersionFailure)]
    [InlineData(true, ExpectedVersionFailure)]
    [InlineData(false, EventAppendFailure)]
    [InlineData(true, EventAppendFailure)]
    [InlineData(false, CursorCommitFailure)]
    [InlineData(true, CursorCommitFailure)]
    [InlineData(false, CallerCancellation)]
    [InlineData(true, CallerCancellation)]
    [InlineData(false, StandaloneReaderControl)]
    [InlineData(true, StandaloneReaderControl)]
    public async Task RecoveredPrefixIsPublishedBeforeTheNextAppendFailsAsync(
        bool large,
        int failureStage
    )
    {
        using CancellationTokenSource caller =
            CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        caller.CancelAfter(TimeSpan.FromSeconds(30));
        BrookKey key = new("test", "recovered-publication");
        BrookPosition committed = new(10);
        BrookPosition cached = new(10);
        CursorStorageModel? pending = new()
        {
            OriginalPosition = new(10),
            Position = new(12),
            ETag = "old-attempt",
        };
        List<BrookCursorMovedEvent> updates = new();
        bool cacheWasCurrentAtNewDispatch = false;
        IOException expectedFailure = new("Controlled failure in the new append.");
        Mock<ICosmosRepository> repository = new(MockBehavior.Strict);
        repository.Setup(r => r.GetPendingCursorDocumentAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => pending);
        repository.Setup(r => r.GetCursorDocumentAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new()
            {
                Position = committed,
                ETag = "committed",
            });
        repository.Setup(r => r.EventExistsAsync(
                key,
                It.IsInRange(11L, 12L, Range.Inclusive),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repository.Setup(r => r.GetExistingEventPositionsAsync(key, 11, 12, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new HashSet<long>
                {
                    11,
                    12,
                });

        async Task CommitRecoveredPrefixAsync()
        {
            committed = new(12);
            pending = null;
            if (failureStage == CallerCancellation)
            {
                await caller.CancelAsync();
            }
        }

        repository.Setup(r => r.CommitCursorPositionAsync(
                key,
                12,
                "old-attempt",
                It.IsAny<Action>(),
                It.IsAny<CancellationToken>()))
            .Returns(CommitRecoveredPrefixAsync);
        repository.Setup(r => r.CommitCursorPositionAsync(key, 12, "old-attempt", It.IsAny<CancellationToken>()))
            .Returns(CommitRecoveredPrefixAsync);
        repository.Setup(r => r.CreatePendingCursorAsync(
                key,
                new(12),
                14,
                It.IsAny<Action>(),
                It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                cacheWasCurrentAtNewDispatch = cached.Value == 12;
                pending = new()
                {
                    OriginalPosition = new(12),
                    Position = new(14),
                    ETag = "new-attempt",
                };
            })
            .ReturnsAsync("new-attempt");
        repository.Setup(r => r.AppendEventBatchAsync(
                key,
                It.IsAny<IReadOnlyList<EventStorageModel>>(),
                It.IsAny<long>(),
                It.IsAny<Action>(),
                It.IsAny<CancellationToken>()))
            .Returns(async () =>
                await (failureStage == EventAppendFailure ? Task.FromException(expectedFailure) : Task.CompletedTask));
        repository.Setup(r => r.CommitCursorPositionAsync(
                key,
                14,
                "new-attempt",
                It.IsAny<Action>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedFailure);
        Mock<IDistributedLock> lease = new(MockBehavior.Strict);
        lease.SetupGet(l => l.LockId).Returns("append-lease");
        lease.Setup(l => l.RenewAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        lease.Setup(l => l.RenewAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        lease.Setup(l => l.DisposeAsync()).Returns(ValueTask.CompletedTask);
        Mock<IDistributedLockManager> locks = new(MockBehavior.Strict);
        locks.Setup(l => l.AcquireLockAsync(key.ToString(), TimeSpan.FromSeconds(60), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lease.Object);
        Func<BrookCursorMovedEvent, CancellationToken, Task> publish = (update, token) =>
        {
            Assert.Equal(key.ToString(), update.BrookKey);
            Assert.Equal(new(12), committed);
            Assert.Null(pending);
            Assert.False(token.CanBeCanceled);
            cached = update.NewPosition;
            updates.Add(update);
            return Task.CompletedTask;
        };
        ServiceCollection services = new();
        services.AddSingleton(publish);
        using ServiceProvider provider = services.BuildServiceProvider();
        DirectRetryPolicy retry = new();
        IOptions<BrookStorageOptions> options = Options.Create(
            new BrookStorageOptions
            {
                MaxEventsPerBatch = large ? 1 : 100,
            });
        BrookRecoveryService recovery = ActivatorUtilities.CreateInstance<BrookRecoveryService>(
            provider,
            repository.Object,
            retry,
            locks.Object,
            options,
            NullLogger<BrookRecoveryService>.Instance);
        if (failureStage == StandaloneReaderControl)
        {
            Assert.Equal(new(12), await recovery.GetOrRecoverCursorPositionAsync(key, caller.Token));
            Assert.Empty(updates);
            Assert.Equal(new(10), cached);
        }
        else
        {
            Mock<IMapper<BrookEvent, EventStorageModel>> mapper = new();
            mapper.Setup(m => m.Map(It.IsAny<BrookEvent>()))
                .Returns((BrookEvent item) => new()
                {
                    EventId = item.Id,
                });
            EventBrookWriter writer = new(
                repository.Object,
                locks.Object,
                new BatchSizeEstimator(),
                retry,
                options,
                mapper.Object,
                recovery,
                NullLogger<EventBrookWriter>.Instance,
                new FakeTimeProvider());
            BrookEvent[] events =
            [
                new()
                {
                    Id = "new-13",
                },
                new()
                {
                    Id = "new-14",
                },
            ];
            if ((failureStage == ExpectedVersionFailure) || (failureStage == CallerCancellation))
            {
                if (failureStage == ExpectedVersionFailure)
                {
                    await Assert.ThrowsAsync<OptimisticConcurrencyException>(async () =>
                        await writer.AppendEventsAsync(key, events, new(10), caller.Token));
                }
                else
                {
                    OperationCanceledException failure =
                        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                            await writer.AppendEventsAsync(key, events, new(12), caller.Token));
                    Assert.Equal(caller.Token, failure.CancellationToken);
                }

                Assert.Null(pending);
            }
            else
            {
                IOException failure = await Assert.ThrowsAsync<IOException>(async () =>
                    await writer.AppendEventsAsync(key, events, new(12), caller.Token));
                Assert.Same(expectedFailure, failure);
                Assert.NotNull(pending);
                Assert.Equal("new-attempt", pending.ETag);
                Assert.True(cacheWasCurrentAtNewDispatch);
            }

            Assert.Equal(new(12), cached);
            Assert.Equal(new(12), Assert.Single(updates).NewPosition);
        }

        Assert.Equal(new(12), committed);
        lease.Verify(l => l.DisposeAsync(), Times.Once);
    }
}