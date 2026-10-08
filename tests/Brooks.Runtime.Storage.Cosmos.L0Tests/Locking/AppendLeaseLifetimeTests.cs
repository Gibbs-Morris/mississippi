using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using Mississippi.Brooks.Runtime.Storage.Cosmos.Locking;

using Moq;


namespace Mississippi.Brooks.Runtime.Storage.Cosmos.L0Tests.Locking;

/// <summary>
///     Verifies actual renewal, conservative deadlines and joined append lease shutdown.
/// </summary>
public sealed class AppendLeaseLifetimeTests
{
    private static readonly DateTimeOffset BaseTime = new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);

    /// <summary>
    ///     Creates a strict lock supporting actual forced renewal only.
    /// </summary>
    /// <returns>The independently verified lock double.</returns>
    private static Mock<IDistributedLock> CreateHealthyLease()
    {
        Mock<IDistributedLock> lease = new(MockBehavior.Strict);
        lease.Setup(l => l.RenewAsync(true, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        return lease;
    }

    /// <summary>
    ///     Creates one supervisor with independently selected supported options.
    /// </summary>
    /// <param name="lease">The acquired lock double.</param>
    /// <param name="clock">The controlled monotonic clock.</param>
    /// <param name="token">The test's caller token.</param>
    /// <param name="duration">The finite duration in seconds.</param>
    /// <param name="threshold">The ordinary renewal threshold in seconds.</param>
    /// <param name="deadlineScheduler">The internal queued deadline scheduler, when supplied.</param>
    /// <returns>The supervisor whose disposal is owned by the calling test.</returns>
    private static AppendLeaseLifetime CreateOwner(
        Mock<IDistributedLock> lease,
        LeaseTestTimeProvider clock,
        CancellationToken token,
        int duration = 60,
        int threshold = 20,
        TaskScheduler? deadlineScheduler = null
    ) =>
        new(
            lease.Object,
            Options.Create(
                new BrookStorageOptions
                {
                    LeaseDurationSeconds = duration,
                    LeaseRenewalThresholdSeconds = threshold,
                }),
            clock,
            token,
            deadlineScheduler);

    /// <summary>
    ///     Bounds a failed fixture without using wall time to drive lease behavior.
    /// </summary>
    /// <returns>The caller-owned fixture watchdog.</returns>
    private static CancellationTokenSource CreateWatchdog()
    {
        CancellationTokenSource watchdog =
            CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        watchdog.CancelAfter(TimeSpan.FromSeconds(10));
        return watchdog;
    }

    /// <summary>
    ///     A storage boundary observes monotonic expiry even when the deadline callback has not run.
    /// </summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task BoundaryRejectsExpiryWhenDeadlineDeliveryIsSuppressedAsync()
    {
        LeaseTestTimeProvider clock = new(BaseTime)
        {
            SuppressDeadline = true,
        };
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Mock<IDistributedLock> lease = new(MockBehavior.Strict);
        lease.SetupSequence(l => l.RenewAsync(true, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Returns(async () =>
            {
                entered.TrySetResult();
                await release.Task.WaitAsync(CancellationToken.None);
            });
        using CancellationTokenSource watchdog = CreateWatchdog();
        AppendLeaseLifetime owner = CreateOwner(lease, clock, watchdog.Token);
        Exception? cleanup = null;
        try
        {
            await owner.StartAsync();
            clock.Advance(TimeSpan.FromSeconds(20));
            await entered.Task.WaitAsync(watchdog.Token);
            clock.Advance(TimeSpan.FromSeconds(39));
            Assert.False(owner.CancellationToken.IsCancellationRequested);
            TimeoutException actual = Assert.Throws<TimeoutException>(owner.ThrowIfFailed);
            Assert.Same(actual, owner.Failure);
            Assert.True(owner.CancellationToken.IsCancellationRequested);
        }
        finally
        {
            release.TrySetResult();
            Task actualDisposal = owner.DisposeAsync().AsTask();
            cleanup = await Record.ExceptionAsync(() => actualDisposal.WaitAsync(CancellationToken.None));
        }

        Assert.IsType<TimeoutException>(cleanup);
        Assert.Equal(0, clock.ActiveTimers);
    }

    /// <summary>
    ///     Caller cancellation stops and joins a healthy supervisor without inventing a lease failure.
    /// </summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task CallerCancellationIsPreservedAsync()
    {
        using CancellationTokenSource caller = CreateWatchdog();
        LeaseTestTimeProvider clock = new(BaseTime);
        Mock<IDistributedLock> lease = CreateHealthyLease();
        await using (AppendLeaseLifetime owner = CreateOwner(lease, clock, caller.Token))
        {
            await owner.StartAsync();
            await caller.CancelAsync();
            OperationCanceledException actual = Assert.Throws<OperationCanceledException>(owner.ThrowIfFailed);
            Assert.Equal(caller.Token, actual.CancellationToken);
            Assert.Null(owner.Failure);
            Assert.False(owner.CanRollback);
        }

        Assert.Equal(0, clock.ActiveTimers);
    }

    /// <summary>
    ///     Disposal awaits an already dispatched deadline callback before token sources are disposed.
    /// </summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task DisposalJoinsHeldDeadlineCallbackAsync()
    {
        using CancellationTokenSource watchdog = CreateWatchdog();
        LeaseTestTimeProvider clock = new(BaseTime)
        {
            HoldDeadline = true,
        };
        Mock<IDistributedLock> lease = CreateHealthyLease();
        await using (AppendLeaseLifetime owner = CreateOwner(lease, clock, watchdog.Token))
        {
            try
            {
                await owner.StartAsync();
                clock.DeliverDeadline();
                await clock.DeadlineEntered.Task.WaitAsync(watchdog.Token);
                Task disposal = owner.DisposeAsync().AsTask();
                await clock.DeadlineDisposalEntered.Task.WaitAsync(watchdog.Token);
                Assert.False(disposal.IsCompleted);
                clock.DeadlineRelease.TrySetResult();
                await disposal.WaitAsync(watchdog.Token);
            }
            finally
            {
                clock.DeadlineRelease.TrySetResult();
            }
        }

        Assert.Equal(0, clock.ActiveTimers);
    }

    /// <summary>
    ///     The initial service request is awaited and a late successful response never admits storage work.
    /// </summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task HeldInitialRenewalExpiresBeforeAdmissionAsync()
    {
        LeaseTestTimeProvider clock = new(BaseTime);
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Mock<IDistributedLock> lease = new(MockBehavior.Strict);
        lease.Setup(l => l.RenewAsync(true, It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                entered.TrySetResult();
                await release.Task.WaitAsync(CancellationToken.None);
            });
        using CancellationTokenSource watchdog = CreateWatchdog();
        AppendLeaseLifetime owner = CreateOwner(lease, clock, watchdog.Token);
        Exception? cleanup = null;
        try
        {
            Task start = owner.StartAsync();
            await entered.Task.WaitAsync(watchdog.Token);
            Assert.False(start.IsCompleted);
            TaskCompletionSource canceled = new(TaskCreationOptions.RunContinuationsAsynchronously);
            using CancellationTokenRegistration registration =
                owner.CancellationToken.Register(() => canceled.TrySetResult());
            clock.Advance(TimeSpan.FromSeconds(59));
            await canceled.Task.WaitAsync(watchdog.Token);
            Assert.True(owner.CancellationToken.IsCancellationRequested);
            Assert.False(start.IsCompleted);
            release.TrySetResult();
            TimeoutException actual = await Assert.ThrowsAsync<TimeoutException>(() => start.WaitAsync(watchdog.Token));
            Assert.Same(actual, owner.Failure);
        }
        finally
        {
            release.TrySetResult();
            Task actualDisposal = owner.DisposeAsync().AsTask();
            cleanup = await Record.ExceptionAsync(() => actualDisposal.WaitAsync(CancellationToken.None));
        }

        Assert.IsType<TimeoutException>(cleanup);
        lease.Verify(l => l.RenewAsync(true, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(0, clock.ActiveTimers);
    }

    /// <summary>
    ///     A held renewal cannot revive expired ownership and its actual task is joined before disposal completes.
    /// </summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task HeldRenewalExpiresAndLateSuccessCannotReviveAsync()
    {
        LeaseTestTimeProvider clock = new(BaseTime);
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource stopObserved = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Mock<IDistributedLock> lease = new(MockBehavior.Strict);
        int calls = 0;
        lease.Setup(l => l.RenewAsync(true, It.IsAny<CancellationToken>()))
            .Returns(async (bool _, CancellationToken token) =>
            {
                if (Interlocked.Increment(ref calls) > 1)
                {
                    using CancellationTokenRegistration
                        registration = token.Register(() => stopObserved.TrySetResult());
                    entered.TrySetResult();
                    await release.Task.WaitAsync(CancellationToken.None);
                }
            });
        using CancellationTokenSource watchdog = CreateWatchdog();
        AppendLeaseLifetime owner = CreateOwner(lease, clock, watchdog.Token);
        TimeoutException? actual = null;
        Exception? cleanup = null;
        try
        {
            await owner.StartAsync();
            clock.Advance(TimeSpan.FromSeconds(20));
            await entered.Task.WaitAsync(watchdog.Token);
            clock.Advance(TimeSpan.FromSeconds(39));
            await stopObserved.Task.WaitAsync(watchdog.Token);
            Assert.True(owner.CancellationToken.IsCancellationRequested);
            Assert.False(owner.CanRollback);
            actual = Assert.Throws<TimeoutException>(owner.ThrowIfFailed);
            Assert.Same(actual, owner.Failure);
        }
        finally
        {
            Task joined = owner.DisposeAsync().AsTask();
            try
            {
                Assert.False(joined.IsCompleted);
            }
            finally
            {
                release.TrySetResult();
                cleanup = await Record.ExceptionAsync(() => joined.WaitAsync(CancellationToken.None));
            }
        }

        Assert.Same(actual, cleanup);
        Assert.Contains(
            "trusted lease window",
            Assert.IsType<TimeoutException>(cleanup).Message,
            StringComparison.Ordinal);
        Assert.Equal(2, calls);
        Assert.Equal(0, clock.ActiveTimers);
    }

    /// <summary>
    ///     Invalid finite durations and thresholds are rejected before acquisition or dispatch.
    /// </summary>
    /// <param name="duration">The invalid duration or its valid control.</param>
    /// <param name="threshold">The invalid threshold or its valid control.</param>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(14, 0)]
    [InlineData(61, 0)]
    [InlineData(-1, 0)]
    [InlineData(60, -1)]
    [InlineData(60, 60)]
    [InlineData(60, 1000)]
    public void InvalidOptionsAreRejected(
        int duration,
        int threshold
    ) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => AppendLeaseLifetime.ValidateOptions(
            new()
            {
                LeaseDurationSeconds = duration,
                LeaseRenewalThresholdSeconds = threshold,
            }));

    /// <summary>
    ///     Cancellation callbacks can inspect already published loss without deadlocking or replacing its cause.
    /// </summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task ReentrantCancellationObservesFirstFailureAsync()
    {
        LeaseTestTimeProvider clock = new(BaseTime);
        TimeoutException failure = new("Unknown renewal outcome");
        Mock<IDistributedLock> lease = new(MockBehavior.Strict);
        lease.SetupSequence(l => l.RenewAsync(true, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .ThrowsAsync(failure);
        TaskCompletionSource callback = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using CancellationTokenSource watchdog = CreateWatchdog();
        AppendLeaseLifetime owner = CreateOwner(lease, clock, watchdog.Token);
        Exception? cleanup = null;
        try
        {
            await owner.StartAsync();
            using CancellationTokenRegistration registration = owner.CancellationToken.Register(() =>
            {
                Assert.Same(failure, owner.Failure);
                Assert.False(owner.CanRollback);
                callback.TrySetResult();
                throw new InvalidOperationException("Secondary cancellation callback failure");
            });
            clock.Advance(TimeSpan.FromSeconds(20));
            await callback.Task.WaitAsync(watchdog.Token);
            Assert.Same(failure, Assert.Throws<TimeoutException>(owner.ThrowIfFailed));
        }
        finally
        {
            Task actualDisposal = owner.DisposeAsync().AsTask();
            cleanup = await Record.ExceptionAsync(() => actualDisposal.WaitAsync(CancellationToken.None));
        }

        Assert.Same(failure, cleanup);
        Assert.Equal(0, clock.ActiveTimers);
    }

    /// <summary>
    ///     Confirmed loss and unknown renewal outcomes retain their first cause and stop all further renewals.
    /// </summary>
    /// <param name="status">The observed provider status represented by the failure.</param>
    /// <returns>The asynchronous test.</returns>
    [Theory]
    [InlineData(404)]
    [InlineData(409)]
    [InlineData(500)]
    public async Task RenewalFailureCancelsWorkAndRetainsCauseAsync(
        int status
    )
    {
        LeaseTestTimeProvider clock = new(BaseTime);
        InvalidOperationException failure = new($"Provider renewal status {status}");
        Mock<IDistributedLock> lease = new(MockBehavior.Strict);
        lease.SetupSequence(l => l.RenewAsync(true, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .ThrowsAsync(failure);
        using CancellationTokenSource watchdog = CreateWatchdog();
        AppendLeaseLifetime owner = CreateOwner(lease, clock, watchdog.Token);
        Exception? cleanup = null;
        try
        {
            await owner.StartAsync();
            TaskCompletionSource canceled = new(TaskCreationOptions.RunContinuationsAsynchronously);
            using CancellationTokenRegistration registration =
                owner.CancellationToken.Register(() => canceled.TrySetResult());
            clock.Advance(TimeSpan.FromSeconds(20));
            await canceled.Task.WaitAsync(watchdog.Token);
            Assert.Same(failure, owner.Failure);
            Assert.False(owner.CanRollback);
            clock.Advance(TimeSpan.FromSeconds(120));
            lease.Verify(l => l.RenewAsync(true, It.IsAny<CancellationToken>()), Times.Exactly(2));
            Assert.Same(failure, Assert.Throws<InvalidOperationException>(owner.ThrowIfFailed));
        }
        finally
        {
            Task actualDisposal = owner.DisposeAsync().AsTask();
            cleanup = await Record.ExceptionAsync(() => actualDisposal.WaitAsync(CancellationToken.None));
        }

        Assert.Same(failure, cleanup);
        Assert.Equal(0, clock.ActiveTimers);
    }

    /// <summary>
    ///     A stale callback uses the latest confirmed window instead of expiring healthy ownership.
    /// </summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task StaleDeadlineUsesLatestConfirmationAsync()
    {
        using CancellationTokenSource watchdog = CreateWatchdog();
        LeaseTestTimeProvider clock = new(BaseTime);
        Mock<IDistributedLock> lease = CreateHealthyLease();
        QueuedDeadlineTaskScheduler scheduler = new();
        await using (AppendLeaseLifetime owner = CreateOwner(
                         lease,
                         clock,
                         watchdog.Token,
                         deadlineScheduler: scheduler))
        {
            await owner.StartAsync();
            Task registration = clock.NextRegistration;
            clock.Advance(TimeSpan.FromSeconds(20));
            await registration.WaitAsync(watchdog.Token);
            clock.DeliverDeadline();
            await scheduler.Queued.Task.WaitAsync(watchdog.Token);
            scheduler.ExecuteNext();
            Assert.Null(owner.Failure);
            Assert.False(owner.CancellationToken.IsCancellationRequested);
            owner.ThrowIfFailed();
        }

        Assert.Equal(0, clock.ActiveTimers);
    }

    /// <summary>
    ///     Every supported option extreme schedules an actual renewal before the trusted deadline.
    /// </summary>
    /// <param name="duration">The configured lease duration.</param>
    /// <param name="threshold">The configured ordinary renewal threshold.</param>
    /// <param name="cadence">The expected conservative cadence in seconds.</param>
    /// <returns>The asynchronous test.</returns>
    [Theory]
    [InlineData(15, 0, 5)]
    [InlineData(15, 14, 1)]
    [InlineData(30, 20, 9)]
    [InlineData(60, 0, 20)]
    [InlineData(60, 20, 20)]
    [InlineData(60, 59, 1)]
    public async Task SupportedOptionsRenewAtConservativeCadenceAsync(
        int duration,
        int threshold,
        int cadence
    )
    {
        using CancellationTokenSource watchdog = CreateWatchdog();
        LeaseTestTimeProvider clock = new(BaseTime);
        Mock<IDistributedLock> lease = CreateHealthyLease();
        await using (AppendLeaseLifetime owner = CreateOwner(lease, clock, watchdog.Token, duration, threshold))
        {
            await owner.StartAsync();
            lease.Verify(l => l.RenewAsync(true, It.IsAny<CancellationToken>()), Times.Once);
            Task registered = clock.NextRegistration;
            clock.Advance(TimeSpan.FromSeconds(cadence));
            await registered.WaitAsync(watchdog.Token);
            lease.Verify(l => l.RenewAsync(true, It.IsAny<CancellationToken>()), Times.Exactly(2));
            lease.Verify(l => l.RenewAsync(It.IsAny<CancellationToken>()), Times.Never);
            Assert.True(owner.CanRollback);
        }

        Assert.Equal(0, clock.ActiveTimers);
    }

    /// <summary>
    ///     UTC wall-clock jumps do not alter monotonic renewal scheduling or ownership.
    /// </summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task UtcJumpDoesNotExpireHealthyLeaseAsync()
    {
        using CancellationTokenSource watchdog = CreateWatchdog();
        LeaseTestTimeProvider clock = new(BaseTime);
        Mock<IDistributedLock> lease = CreateHealthyLease();
        await using (AppendLeaseLifetime owner = CreateOwner(lease, clock, watchdog.Token))
        {
            await owner.StartAsync();
            clock.UtcOffset = TimeSpan.FromDays(30);
            owner.ThrowIfFailed();
            Task registration = clock.NextRegistration;
            clock.Advance(TimeSpan.FromSeconds(20));
            await registration.WaitAsync(watchdog.Token);
            lease.Verify(l => l.RenewAsync(true, It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        Assert.Equal(0, clock.ActiveTimers);
    }
}