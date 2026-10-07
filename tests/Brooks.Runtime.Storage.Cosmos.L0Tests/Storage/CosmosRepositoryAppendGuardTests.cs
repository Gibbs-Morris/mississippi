using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Runtime.Storage.Cosmos.L0Tests.Locking;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Locking;
using Mississippi.Brooks.Runtime.Storage.Cosmos.Storage;
using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Common.Runtime.Storage.Abstractions.Retry;

using Moq;


namespace Mississippi.Brooks.Runtime.Storage.Cosmos.L0Tests.Storage;

/// <summary>
///     Verifies explicit append ownership checks at each direct Cosmos mutation and retry boundary.
/// </summary>
public sealed class CosmosRepositoryAppendGuardTests
{
    private static readonly BrookKey Key = new("test", "direct-guard");

    /// <summary>
    ///     Creates a real repository with an independently controlled provider and retry policy.
    /// </summary>
    /// <param name="container">The SDK boundary double.</param>
    /// <param name="retryPolicy">An optional custom retry double.</param>
    /// <returns>The repository being exercised.</returns>
    private static CosmosRepository CreateRepository(
        Container container,
        IRetryPolicy? retryPolicy = null
    )
    {
        if (retryPolicy is null)
        {
            Mock<IRetryPolicy> retry = new();
            retry.Setup(r => r.ExecuteAsync(It.IsAny<Func<Task<bool>>>(), It.IsAny<CancellationToken>()))
                .Returns((Func<Task<bool>> operation, CancellationToken _) => operation());
            retry.Setup(r => r.ExecuteAsync(
                    It.IsAny<Func<Task<ItemResponse<CursorDocument>>>>(),
                    It.IsAny<CancellationToken>()))
                .Returns((Func<Task<ItemResponse<CursorDocument>>> operation, CancellationToken _) => operation());
            retryPolicy = retry.Object;
        }

        return new(
            container,
            retryPolicy,
            Mock.Of<IMapper<CursorDocument, CursorStorageModel>>(),
            Mock.Of<IMapper<EventDocument, EventStorageModel>>());
    }

    /// <summary>
    ///     Bounds failed fixture waits without driving lease time.
    /// </summary>
    /// <returns>The test-owned watchdog.</returns>
    private static CancellationTokenSource CreateWatchdog()
    {
        CancellationTokenSource watchdog =
            CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        watchdog.CancelAfter(TimeSpan.FromSeconds(10));
        return watchdog;
    }

    /// <summary>
    ///     Every guarded mutation rejects an already canceled caller before SDK dispatch even if the SDK would ignore it.
    /// </summary>
    /// <param name="operation">The guarded mutation to exercise.</param>
    /// <returns>The asynchronous test.</returns>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task CanceledGuardedMutationDoesNotDispatchAsync(
        int operation
    )
    {
        using CancellationTokenSource caller = CreateWatchdog();
        await caller.CancelAsync();
        Mock<Container> container = new(MockBehavior.Strict);
        CosmosRepository repository = CreateRepository(container.Object);
        int checks = 0;
        Action guard = () => checks++;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation switch
        {
            0 => repository.AppendEventBatchAsync(Key, [new()], 1, guard, caller.Token),
            1 => repository.CreatePendingCursorAsync(Key, new(0), 1, guard, caller.Token),
            2 => repository.CommitCursorPositionAsync(Key, 1, guard, caller.Token),
            3 => repository.DeleteEventAsync(Key, 1, guard, caller.Token),
            var _ => repository.DeletePendingCursorAsync(Key, guard, caller.Token),
        });
        Assert.Equal(1, checks);
        container.VerifyNoOtherCalls();
    }

    /// <summary>
    ///     Delayed timer delivery cannot admit an event2 create or pending delete after a held mutation completes.
    /// </summary>
    /// <param name="commit">Whether the held mutation is cursor upsert instead of event1.</param>
    /// <returns>The asynchronous test.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DirectBoundaryRejectsExpiredLeaseWithoutTimerCallbackAsync(
        bool commit
    )
    {
        LeaseTestTimeProvider clock = new(new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero))
        {
            SuppressDeadline = true,
        };
        TaskCompletionSource mutationEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource finishMutation = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource renewalEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource finishRenewal = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Mock<IDistributedLock> lease = new(MockBehavior.Strict);
        lease.SetupSequence(l => l.RenewAsync(true, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Returns(async () =>
            {
                renewalEntered.TrySetResult();
                await finishRenewal.Task.WaitAsync(CancellationToken.None);
            });
        Mock<Container> container = new(MockBehavior.Strict);
        if (commit)
        {
            container.Setup(c => c.UpsertItemAsync(
                    It.IsAny<CursorDocument>(),
                    It.IsAny<PartitionKey>(),
                    null,
                    It.IsAny<CancellationToken>()))
                .Returns(async () =>
                {
                    mutationEntered.TrySetResult();
                    await finishMutation.Task.WaitAsync(CancellationToken.None);
                    return Mock.Of<ItemResponse<CursorDocument>>();
                });
        }
        else
        {
            container.Setup(c => c.CreateItemAsync(
                    It.IsAny<EventDocument>(),
                    It.IsAny<PartitionKey>(),
                    null,
                    It.IsAny<CancellationToken>()))
                .Returns(async () =>
                {
                    mutationEntered.TrySetResult();
                    await finishMutation.Task.WaitAsync(CancellationToken.None);
                    return Mock.Of<ItemResponse<EventDocument>>();
                });
        }

        CosmosRepository repository = CreateRepository(container.Object);
        using CancellationTokenSource watchdog = CreateWatchdog();
        AppendLeaseLifetime owner = new(lease.Object, Options.Create(new BrookStorageOptions()), clock, watchdog.Token);
        Task? mutation = null;
        Exception? cleanup = null;
        try
        {
            await owner.StartAsync();
            mutation = commit
                ? repository.CommitCursorPositionAsync(Key, 2, owner.ThrowIfFailed, owner.CancellationToken)
                : repository.AppendEventBatchAsync(
                    Key,
                    [
                        new()
                        {
                            EventId = "event1",
                        },
                        new()
                        {
                            EventId = "event2",
                        },
                    ],
                    1,
                    owner.ThrowIfFailed,
                    owner.CancellationToken);
            Task ownedMutation = mutation;
            await mutationEntered.Task.WaitAsync(watchdog.Token);
            clock.Advance(TimeSpan.FromSeconds(20));
            await renewalEntered.Task.WaitAsync(watchdog.Token);
            clock.Advance(TimeSpan.FromSeconds(39));
            Assert.False(owner.CancellationToken.IsCancellationRequested);
            finishMutation.TrySetResult();
            await Assert.ThrowsAsync<TimeoutException>(() => ownedMutation.WaitAsync(CancellationToken.None));
            Assert.True(owner.CancellationToken.IsCancellationRequested);
            Assert.False(owner.CanRollback);
        }
        finally
        {
            finishMutation.TrySetResult();
            finishRenewal.TrySetResult();
            if (mutation is not null)
            {
                Task observedMutation = mutation;
                await Record.ExceptionAsync(async () => await observedMutation.WaitAsync(CancellationToken.None));
            }

            Task actualDisposal = owner.DisposeAsync().AsTask();
            cleanup = await Record.ExceptionAsync(() => actualDisposal.WaitAsync(CancellationToken.None));
        }

        Assert.IsType<TimeoutException>(cleanup);
        container.Verify(
            c => c.DeleteItemAsync<CursorDocument>(
                It.IsAny<string>(),
                It.IsAny<PartitionKey>(),
                null,
                It.IsAny<CancellationToken>()),
            Times.Never);
        container.Verify(
            c => c.CreateItemAsync(
                It.IsAny<EventDocument>(),
                It.IsAny<PartitionKey>(),
                null,
                It.IsAny<CancellationToken>()),
            commit ? Times.Never() : Times.Once());
        container.Verify(
            c => c.UpsertItemAsync(
                It.IsAny<CursorDocument>(),
                It.IsAny<PartitionKey>(),
                null,
                It.IsAny<CancellationToken>()),
            commit ? Times.Once() : Times.Never());
        Assert.Equal(0, clock.ActiveTimers);
    }

    /// <summary>
    ///     A retry double ignoring cancellation still invokes the guard before its second SDK attempt.
    /// </summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task EveryRetryInvokesExplicitGuardAsync()
    {
        Mock<Container> container = new(MockBehavior.Strict);
        container.Setup(c => c.CreateItemAsync(
                It.IsAny<EventDocument>(),
                It.IsAny<PartitionKey>(),
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("First SDK attempt failed"));
        Mock<IRetryPolicy> retry = new(MockBehavior.Strict);
        retry.Setup(r => r.ExecuteAsync(It.IsAny<Func<Task<bool>>>(), It.IsAny<CancellationToken>()))
            .Returns(async (Func<Task<bool>> operation, CancellationToken _) =>
            {
                try
                {
                    return await operation();
                }
                catch (InvalidOperationException)
                {
                    return await operation();
                }
            });
        CosmosRepository repository = CreateRepository(container.Object, retry.Object);
        TimeoutException leaseFailure = new("Lease expired before retry");
        int checks = 0;
        TimeoutException actual = await Assert.ThrowsAsync<TimeoutException>(() => repository.AppendEventBatchAsync(
            Key,
            [new()],
            1,
            () =>
            {
                if (++checks == 2)
                {
                    throw leaseFailure;
                }
            },
            TestContext.Current.CancellationToken));
        Assert.Same(leaseFailure, actual);
        Assert.Equal(2, checks);
        container.Verify(
            c => c.CreateItemAsync(
                It.IsAny<EventDocument>(),
                It.IsAny<PartitionKey>(),
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    ///     Explicit guards are confined to their individual calls and an ordinary standalone call remains usable.
    /// </summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task GuardFailureDoesNotAffectAnotherOrStandaloneCallAsync()
    {
        Mock<Container> container = new(MockBehavior.Strict);
        container.Setup(c => c.CreateItemAsync(
                It.IsAny<EventDocument>(),
                It.IsAny<PartitionKey>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<ItemResponse<EventDocument>>());
        CosmosRepository repository = CreateRepository(container.Object);
        await Assert.ThrowsAsync<TimeoutException>(() => repository.AppendEventBatchAsync(
            Key,
            [new()],
            1,
            () => throw new TimeoutException("Only the first append lost ownership"),
            TestContext.Current.CancellationToken));
        int healthyChecks = 0;
        await repository.AppendEventBatchAsync(
            Key,
            [new()],
            2,
            () => healthyChecks++,
            TestContext.Current.CancellationToken);
        await repository.AppendEventBatchAsync(Key, [new()], 3, TestContext.Current.CancellationToken);
        Assert.Equal(1, healthyChecks);
        container.Verify(
            c => c.CreateItemAsync(
                It.IsAny<EventDocument>(),
                It.IsAny<PartitionKey>(),
                null,
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }
}