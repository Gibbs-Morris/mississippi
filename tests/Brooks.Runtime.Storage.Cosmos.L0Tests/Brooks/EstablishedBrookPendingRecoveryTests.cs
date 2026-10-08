using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
///     Verifies recovery of an interrupted append above an existing committed cursor.
/// </summary>
public sealed class EstablishedBrookPendingRecoveryTests
{
    private sealed class TestRetryPolicy : IRetryPolicy
    {
        /// <summary>
        ///     Executes each recorded repository operation directly.
        /// </summary>
        /// <typeparam name="T">The operation result.</typeparam>
        /// <param name="operation">The repository operation.</param>
        /// <param name="cancellationToken">The operation token.</param>
        /// <returns>The repository result.</returns>
        public Task<T> ExecuteAsync<T>(
            Func<Task<T>> operation,
            CancellationToken cancellationToken = default
        ) =>
            operation();
    }

    /// <summary>
    ///     A new append lease recovers the pending state left by renewal loss on a non-empty brook.
    /// </summary>
    /// <param name="large">Whether to split the two events into multiple batches.</param>
    /// <returns>The asynchronous test.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NextAppendRecoversPendingAfterRenewalLossAsync(
        bool large
    )
    {
        using CancellationTokenSource watchdog =
            CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        watchdog.CancelAfter(TimeSpan.FromSeconds(10));
        BrookKey key = new("test", "established-pending");
        LeaseTestTimeProvider clock = new(new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero));
        TaskCompletionSource pendingCreated = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource finishPending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource leaseLost = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TimeoutException renewalFailure = new("The first append renewal is unknown");
        Mock<IDistributedLock> firstLease = new(MockBehavior.Strict);
        int firstRenewals = 0;
        firstLease.Setup(l => l.RenewAsync(true, It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                if (++firstRenewals > 1)
                {
                    throw renewalFailure;
                }

                return Task.CompletedTask;
            });
        firstLease.Setup(l => l.DisposeAsync()).Returns(default(ValueTask));
        Mock<IDistributedLock> secondLease = new(MockBehavior.Strict);
        secondLease.Setup(l => l.RenewAsync(true, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        secondLease.Setup(l => l.DisposeAsync()).Returns(default(ValueTask));
        Mock<IDistributedLockManager> locks = new(MockBehavior.Strict);
        int acquisitions = 0;
        locks.Setup(l => l.AcquireLockAsync(key.ToString(), TimeSpan.FromSeconds(60), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => ++acquisitions == 1 ? firstLease.Object : secondLease.Object);
        long committed = 10;
        CursorStorageModel? pending = null;
        Mock<ICosmosRepository> repository = new(MockBehavior.Strict);
        repository.Setup(r => r.GetCursorDocumentAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new()
            {
                Position = new(committed),
            });
        repository.Setup(r => r.GetPendingCursorDocumentAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => pending);
        int pendingWrites = 0;
        repository.Setup(r => r.CreatePendingCursorAsync(
                key,
                It.IsAny<BrookPosition>(),
                It.IsAny<long>(),
                It.IsAny<Action>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (BrookKey _, BrookPosition original, long target, Action guard, CancellationToken token) =>
            {
                guard();
                token.ThrowIfCancellationRequested();
                if (pending is not null)
                {
                    throw new InvalidOperationException("Pending cursor already exists (409).");
                }

                pending = new()
                {
                    OriginalPosition = original,
                    Position = new(target),
                };
                if (++pendingWrites == 1)
                {
                    using CancellationTokenRegistration registration = token.Register(() => leaseLost.TrySetResult());
                    pendingCreated.TrySetResult();
                    await finishPending.Task.WaitAsync(CancellationToken.None);
                }
            });
        List<long> appended = new();
        repository.Setup(r => r.AppendEventBatchAsync(
                key,
                It.IsAny<IReadOnlyList<EventStorageModel>>(),
                It.IsAny<long>(),
                It.IsAny<Action>(),
                It.IsAny<CancellationToken>()))
            .Returns((
                BrookKey _, IReadOnlyList<EventStorageModel> batch, long start, Action guard, CancellationToken token
            ) =>
            {
                guard();
                token.ThrowIfCancellationRequested();
                for (int index = 0; index < batch.Count; index++)
                {
                    appended.Add(start + index);
                }

                return Task.CompletedTask;
            });
        repository.Setup(r => r.EventExistsAsync(key, It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        List<long> deleted = new();
        repository.Setup(r => r.DeleteEventAsync(
                key,
                It.IsAny<long>(),
                It.IsAny<Action>(),
                It.IsAny<CancellationToken>()))
            .Returns((BrookKey _, long position, Action guard, CancellationToken token) =>
            {
                guard();
                token.ThrowIfCancellationRequested();
                deleted.Add(position);
                return Task.CompletedTask;
            });
        repository.Setup(r => r.DeletePendingCursorAsync(key, It.IsAny<Action>(), It.IsAny<CancellationToken>()))
            .Returns((BrookKey _, Action guard, CancellationToken token) =>
            {
                guard();
                token.ThrowIfCancellationRequested();
                pending = null;
                return Task.CompletedTask;
            });
        repository.Setup(r => r.CommitCursorPositionAsync(
                key,
                It.IsAny<long>(),
                It.IsAny<Action>(),
                It.IsAny<CancellationToken>()))
            .Returns((BrookKey _, long position, Action guard, CancellationToken token) =>
            {
                guard();
                token.ThrowIfCancellationRequested();
                committed = position;
                pending = null;
                return Task.CompletedTask;
            });
        IOptions<BrookStorageOptions> options = Options.Create(
            new BrookStorageOptions
            {
                MaxEventsPerBatch = large ? 1 : 100,
            });
        TestRetryPolicy retry = new();
        BrookRecoveryService recovery = new(
            repository.Object,
            retry,
            locks.Object,
            options,
            NullLogger<BrookRecoveryService>.Instance);
        Mock<IMapper<BrookEvent, EventStorageModel>> mapper = new();
        mapper.Setup(m => m.Map(It.IsAny<BrookEvent>()))
            .Returns((BrookEvent e) => new()
            {
                EventId = e.Id,
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
            clock);
        BrookEvent[] events =
        [
            new()
            {
                Id = "event-11",
            },
            new()
            {
                Id = "event-12",
            },
        ];
        Task<BrookPosition> firstAppend = writer.AppendEventsAsync(key, events, new(10), watchdog.Token);
        try
        {
            await pendingCreated.Task.WaitAsync(watchdog.Token);
            clock.Advance(TimeSpan.FromSeconds(20));
            await leaseLost.Task.WaitAsync(watchdog.Token);
            finishPending.TrySetResult();
            TimeoutException actual =
                await Assert.ThrowsAsync<TimeoutException>(() => firstAppend.WaitAsync(watchdog.Token));
            Assert.Same(renewalFailure, actual);
        }
        finally
        {
            finishPending.TrySetResult();
            await Record.ExceptionAsync(() => firstAppend.WaitAsync(CancellationToken.None));
        }

        Assert.Equal(10, committed);
        Assert.NotNull(pending);
        Assert.Equal(12, pending.Position.Value);
        Assert.Empty(appended);
        Assert.Empty(deleted);
        Assert.Equal(0, clock.ActiveTimers);
        Assert.Equal(12, (await writer.AppendEventsAsync(key, events, new(10), watchdog.Token)).Value);
        Assert.Equal(new long[] { 11, 12 }, deleted);
        Assert.Equal(new long[] { 11, 12 }, appended);
        Assert.Equal(12, committed);
        Assert.Null(pending);
        repository.Verify(
            r => r.CommitCursorPositionAsync(key, 12, It.IsAny<Action>(), It.IsAny<CancellationToken>()),
            Times.Once);
        repository.Verify(
            r => r.DeletePendingCursorAsync(key, It.IsAny<Action>(), It.IsAny<CancellationToken>()),
            Times.Once);
        firstLease.Verify(l => l.DisposeAsync(), Times.Once);
        secondLease.Verify(l => l.DisposeAsync(), Times.Once);
        locks.Verify(
            l => l.AcquireLockAsync(key.ToString(), TimeSpan.FromSeconds(60), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        locks.VerifyNoOtherCalls();
        Assert.Equal(0, clock.ActiveTimers);
    }

    /// <summary>
    ///     Lost append ownership blocks pending repair before its next commit or compensating deletion.
    /// </summary>
    /// <param name="complete">Whether repair would commit or roll back.</param>
    /// <returns>The asynchronous test.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PendingRepairChecksOwnershipBeforeMutationAsync(
        bool complete
    )
    {
        BrookKey key = new("test", "lost-pending-repair");
        Mock<ICosmosRepository> repository = new(MockBehavior.Strict);
        repository.Setup(r => r.GetCursorDocumentAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new CursorStorageModel
                {
                    Position = new(10),
                });
        repository.Setup(r => r.GetPendingCursorDocumentAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new CursorStorageModel
                {
                    OriginalPosition = new(10),
                    Position = new(12),
                });
        bool lost = false;
        repository.Setup(r => r.EventExistsAsync(key, It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Returns((BrookKey _, long position, CancellationToken _) =>
            {
                lost = !complete || (position == 12);
                return Task.FromResult(complete);
            });
        Mock<IDistributedLockManager> locks = new(MockBehavior.Strict);
        BrookRecoveryService recovery = new(
            repository.Object,
            new TestRetryPolicy(),
            locks.Object,
            Options.Create(new BrookStorageOptions()),
            NullLogger<BrookRecoveryService>.Instance);
        TimeoutException failure = new("Append ownership lost during pending repair");
        TimeoutException actual = await Assert.ThrowsAsync<TimeoutException>(() =>
            recovery.GetOrRecoverCursorPositionAsync(
                key,
                () =>
                {
                    if (lost)
                    {
                        throw failure;
                    }
                },
                TestContext.Current.CancellationToken));
        Assert.Same(failure, actual);
        repository.Verify(
            r => r.CommitCursorPositionAsync(key, It.IsAny<long>(), It.IsAny<Action>(), It.IsAny<CancellationToken>()),
            Times.Never);
        repository.Verify(
            r => r.DeleteEventAsync(key, It.IsAny<long>(), It.IsAny<Action>(), It.IsAny<CancellationToken>()),
            Times.Never);
        repository.Verify(
            r => r.DeletePendingCursorAsync(key, It.IsAny<Action>(), It.IsAny<CancellationToken>()),
            Times.Never);
        locks.VerifyNoOtherCalls();
    }

    /// <summary>
    ///     Pending repair never deletes committed history when its original position is stale.
    /// </summary>
    /// <param name="complete">Whether all uncommitted events already exist.</param>
    /// <returns>The asynchronous test.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PendingRepairPreservesCommittedHistoryAsync(
        bool complete
    )
    {
        BrookKey key = new("test", "stale-original-pending");
        Mock<ICosmosRepository> repository = new(MockBehavior.Strict);
        Mock<IDistributedLockManager> locks = new(MockBehavior.Strict);
        long committed = 10;
        bool hasPending = true;
        repository.Setup(r => r.GetCursorDocumentAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new()
            {
                Position = new(committed),
            });
        repository.Setup(r => r.GetPendingCursorDocumentAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new CursorStorageModel
                {
                    OriginalPosition = new(0),
                    Position = new(12),
                });
        List<long> checkedPositions = new();
        repository.Setup(r => r.EventExistsAsync(key, It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Returns((BrookKey _, long position, CancellationToken _) =>
            {
                checkedPositions.Add(position);
                return Task.FromResult(complete);
            });
        List<long> deleted = new();
        repository.Setup(r => r.DeleteEventAsync(
                key,
                It.IsAny<long>(),
                It.IsAny<Action>(),
                It.IsAny<CancellationToken>()))
            .Returns((BrookKey _, long position, Action guard, CancellationToken _) =>
            {
                guard();
                deleted.Add(position);
                return Task.CompletedTask;
            });
        repository.Setup(r => r.DeletePendingCursorAsync(key, It.IsAny<Action>(), It.IsAny<CancellationToken>()))
            .Returns((BrookKey _, Action guard, CancellationToken _) =>
            {
                guard();
                hasPending = false;
                return Task.CompletedTask;
            });
        repository.Setup(r => r.CommitCursorPositionAsync(key, 12, It.IsAny<Action>(), It.IsAny<CancellationToken>()))
            .Returns((BrookKey _, long position, Action guard, CancellationToken _) =>
            {
                guard();
                committed = position;
                hasPending = false;
                return Task.CompletedTask;
            });
        repository.Setup(r => r.CreatePendingCursorAsync(
                key,
                It.IsAny<BrookPosition>(),
                It.IsAny<long>(),
                It.IsAny<Action>(),
                It.IsAny<CancellationToken>()))
            .Returns(() =>
                hasPending
                    ? Task.FromException(new InvalidOperationException("Pending cursor already exists (409)."))
                    : Task.CompletedTask);
        BrookRecoveryService recovery = new(
            repository.Object,
            new TestRetryPolicy(),
            locks.Object,
            Options.Create(new BrookStorageOptions()),
            NullLogger<BrookRecoveryService>.Instance);
        Action guard = () => TestContext.Current.CancellationToken.ThrowIfCancellationRequested();
        BrookPosition position = await recovery.GetOrRecoverCursorPositionAsync(
            key,
            guard,
            TestContext.Current.CancellationToken);
        await repository.Object.CreatePendingCursorAsync(
            key,
            position,
            position.Value + 1,
            guard,
            TestContext.Current.CancellationToken);
        Assert.Equal(complete ? 12 : 10, position.Value);
        Assert.Equal(complete ? new long[] { 11, 12 } : new long[] { 11 }, checkedPositions);
        Assert.Equal(complete ? Array.Empty<long>() : new long[] { 11, 12 }, deleted);
        Assert.False(hasPending);
        repository.Verify(
            r => r.CommitCursorPositionAsync(key, 12, guard, It.IsAny<CancellationToken>()),
            complete ? Times.Once() : Times.Never());
        repository.Verify(
            r => r.DeletePendingCursorAsync(key, guard, It.IsAny<CancellationToken>()),
            complete ? Times.Never() : Times.Once());
        locks.VerifyNoOtherCalls();
    }
}