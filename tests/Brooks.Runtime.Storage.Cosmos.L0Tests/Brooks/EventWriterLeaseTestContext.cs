using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
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
///     Provides independently released storage and renewal boundaries for real writer lifetime tests.
/// </summary>
internal sealed class EventWriterLeaseTestContext
{
    private int renewals;

    /// <summary>
    ///     Initializes a new instance of the <see cref="EventWriterLeaseTestContext" /> class.
    /// </summary>
    /// <param name="clock">The test's independently advanced clock.</param>
    /// <param name="options">The supported append configuration.</param>
    /// <param name="deadlineScheduler">The internal queued deadline scheduler, when supplied.</param>
    public EventWriterLeaseTestContext(
        LeaseTestTimeProvider clock,
        IOptions<BrookStorageOptions> options,
        TaskScheduler? deadlineScheduler = null
    )
    {
        Clock = clock;
        Lease.Setup(l => l.RenewAsync(true, It.IsAny<CancellationToken>()))
            .Returns((bool _, CancellationToken token) => RenewAsync(token));
        Lease.Setup(l => l.DisposeAsync())
            .Returns(() =>
            {
                if (ReleaseFailure is Exception failure)
                {
                    return ValueTask.FromException(failure);
                }

                return default;
            });
        Mock<IDistributedLockManager> locks = new(MockBehavior.Strict);
        locks.Setup(l => l.AcquireLockAsync(
                Key.ToString(),
                TimeSpan.FromSeconds(options.Value.LeaseDurationSeconds),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Lease.Object);
        Mock<IRetryPolicy> retry = new(MockBehavior.Strict);
        retry.Setup(r => r.ExecuteAsync(It.IsAny<Func<Task<bool>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<Task<bool>> operation, CancellationToken _) => operation());
        Mock<IMapper<BrookEvent, EventStorageModel>> mapper = new();
        mapper.Setup(m => m.Map(It.IsAny<BrookEvent>()))
            .Returns((BrookEvent e) => new()
            {
                EventId = e.Id,
            });
        Mock<IBrookRecoveryService> recovery = new(MockBehavior.Strict);
        recovery.Setup(r => r.GetOrRecoverCursorPositionAsync(Key, It.IsAny<Action>(), It.IsAny<CancellationToken>()))
            .Returns(async (BrookKey _, Action guard, CancellationToken token) =>
            {
                WorkToken = token;
                guard();
                await HoldAsync(0);
                return new(0);
            });
        Repository
            .Setup(r => r.CreatePendingCursorAsync(Key, new(0), 2, It.IsAny<Action>(), It.IsAny<CancellationToken>()))
            .Returns(async (BrookKey _, BrookPosition _, long _, Action guard, CancellationToken _) =>
            {
                guard();
                await HoldAsync(1);
            });
        Repository.Setup(r => r.AppendEventBatchAsync(
                Key,
                It.IsAny<IReadOnlyList<EventStorageModel>>(),
                It.IsAny<long>(),
                It.IsAny<Action>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (
                BrookKey _, IReadOnlyList<EventStorageModel> _, long position, Action guard, CancellationToken _
            ) =>
            {
                guard();
                AppendedPositions.Add(position);
                if (position == 1)
                {
                    await HoldAsync(2);
                    if (StorageFailure is Exception failure)
                    {
                        throw failure;
                    }
                }
            });
        Repository.Setup(r => r.CommitCursorPositionAsync(Key, 2, It.IsAny<Action>(), It.IsAny<CancellationToken>()))
            .Returns(async (BrookKey _, long _, Action guard, CancellationToken _) =>
            {
                guard();
                await HoldAsync(3);
                CommitCompleted?.Invoke();
            });
        Repository.Setup(r => r.DeleteEventAsync(
                Key,
                It.IsAny<long>(),
                It.IsAny<Action>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        Repository.Setup(r => r.DeletePendingCursorAsync(Key, It.IsAny<Action>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        Repository.Setup(r => r.EventExistsAsync(Key, It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        Writer = new(
            Repository.Object,
            locks.Object,
            new BatchSizeEstimator(),
            retry.Object,
            options,
            mapper.Object,
            recovery.Object,
            Logger.Object,
            clock,
            deadlineScheduler);
    }

    /// <summary>
    ///     Gets the event positions dispatched by the repository double.
    /// </summary>
    public List<long> AppendedPositions { get; } = new();

    /// <summary>
    ///     Gets the controlled clock.
    /// </summary>
    public LeaseTestTimeProvider Clock { get; }

    /// <summary>
    ///     Gets or sets the action delivered after the successful commit request completes.
    /// </summary>
    public Action? CommitCompleted { get; set; }

    /// <summary>
    ///     Gets the two stable events used in either batch path.
    /// </summary>
    public IReadOnlyList<BrookEvent> Events { get; } =
    [
        new()
        {
            Id = "event1",
        },
        new()
        {
            Id = "event2",
        },
    ];

    /// <summary>
    ///     Gets the explicit release for the provider request that ignores cancellation.
    /// </summary>
    public TaskCompletionSource FinishRenewal { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    ///     Gets the explicit storage request release.
    /// </summary>
    public TaskCompletionSource FinishStorage { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    ///     Gets or sets which storage boundary is held: recovery0, pending1, events2 or commit3.
    /// </summary>
    public int HeldBoundary { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether actual renewal ignores cancellation until explicitly released.
    /// </summary>
    public bool HoldRenewal { get; set; }

    /// <summary>
    ///     Gets the stable test brook identifier.
    /// </summary>
    public BrookKey Key { get; } = new("test", "writer-boundary");

    /// <summary>
    ///     Gets the append lease double, whose disposal is owned by the writer.
    /// </summary>
    public Mock<IDistributedLock> Lease { get; } = new(MockBehavior.Strict);

    /// <summary>
    ///     Gets the writer's capturing logger, disabled by default.
    /// </summary>
    public Mock<ILogger<EventBrookWriter>> Logger { get; } = new();

    /// <summary>
    ///     Gets or sets a secondary lease release failure.
    /// </summary>
    public Exception? ReleaseFailure { get; set; }

    /// <summary>
    ///     Gets the barrier proving actual renewal entry.
    /// </summary>
    public TaskCompletionSource RenewalEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    ///     Gets or sets a provider failure after the initial healthy renewal.
    /// </summary>
    public Exception? RenewalFailure { get; set; }

    /// <summary>
    ///     Gets the barrier proving shutdown canceled an actual held renewal request.
    /// </summary>
    public TaskCompletionSource RenewalStopObserved { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    ///     Gets the number of actual forced renewals.
    /// </summary>
    public int Renewals => Volatile.Read(ref renewals);

    /// <summary>
    ///     Gets the strict repository boundary double.
    /// </summary>
    public Mock<ICosmosRepository> Repository { get; } = new(MockBehavior.Strict);

    /// <summary>
    ///     Gets the specific storage entry barrier.
    /// </summary>
    public TaskCompletionSource StorageEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    ///     Gets or sets a primary event storage failure after its held request returns.
    /// </summary>
    public Exception? StorageFailure { get; set; }

    /// <summary>
    ///     Gets the linked caller/loss token passed to actual writer work.
    /// </summary>
    public CancellationToken WorkToken { get; private set; }

    /// <summary>
    ///     Gets the real writer under test.
    /// </summary>
    public EventBrookWriter Writer { get; }

    /// <summary>
    ///     Holds only the selected storage boundary until its explicit release.
    /// </summary>
    /// <param name="boundary">The entered boundary.</param>
    /// <returns>The owned storage request.</returns>
    private async Task HoldAsync(
        int boundary
    )
    {
        if (boundary == HeldBoundary)
        {
            StorageEntered.TrySetResult();
            await FinishStorage.Task.WaitAsync(CancellationToken.None);
        }
    }

    /// <summary>
    ///     Records each actual request and deliberately joins a non-cooperating request when configured.
    /// </summary>
    /// <param name="token">The supervisor's actual request cancellation token.</param>
    /// <returns>The actual provider request.</returns>
    private async Task RenewAsync(
        CancellationToken token
    )
    {
        if (Interlocked.Increment(ref renewals) == 1)
        {
            return;
        }

        RenewalEntered.TrySetResult();
        if (HoldRenewal)
        {
            using CancellationTokenRegistration registration = token.Register(() => RenewalStopObserved.TrySetResult());
            await FinishRenewal.Task.WaitAsync(CancellationToken.None);
        }

        if (RenewalFailure is Exception failure)
        {
            throw failure;
        }
    }
}