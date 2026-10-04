using System;
using System.Threading;
using System.Threading.Tasks;

using Azure;

using Microsoft.Extensions.Options;

using Mississippi.Brooks.Abstractions;
using Mississippi.Brooks.Runtime.Storage.Cosmos.L0Tests.Locking;

using Moq;


namespace Mississippi.Brooks.Runtime.Storage.Cosmos.L0Tests.Brooks;

/// <summary>
///     Verifies renewal through awaited writer boundaries, terminal cancellation and primary-error protection.
/// </summary>
public sealed class EventBrookWriterLeaseBoundaryTests
{
    /// <summary>
    ///     Asserts that an interrupted append leaves commit and compensating mutation paths unused.
    /// </summary>
    /// <param name="context">The recorded writer boundary context.</param>
    private static void AssertNoCommitOrDeletes(
        EventWriterLeaseTestContext context
    )
    {
        context.Repository.Verify(
            r => r.CommitCursorPositionAsync(
                context.Key,
                It.IsAny<long>(),
                It.IsAny<Action>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        context.Repository.Verify(
            r => r.DeleteEventAsync(context.Key, It.IsAny<long>(), It.IsAny<Action>(), It.IsAny<CancellationToken>()),
            Times.Never);
        context.Repository.Verify(
            r => r.DeletePendingCursorAsync(context.Key, It.IsAny<Action>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    ///     Creates either batch path with supported options and a fixed independent clock.
    /// </summary>
    /// <param name="large">Whether to split the two events into two batches.</param>
    /// <param name="deadlineScheduler">The internal queued scheduler, when supplied.</param>
    /// <returns>The independently configured writer fixture.</returns>
    private static EventWriterLeaseTestContext CreateContext(
        bool large,
        TaskScheduler? deadlineScheduler = null
    ) =>
        new(
            new(new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero)),
            Options.Create(
                new BrookStorageOptions
                {
                    MaxEventsPerBatch = large ? 1 : 100,
                }),
            deadlineScheduler);

    /// <summary>
    ///     Bounds failed fixture waits without controlling virtual lease time.
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
    ///     Caller cancellation cannot launch another mutation or compensate an uncertain completed request.
    /// </summary>
    /// <param name="large">Whether to force multiple batches.</param>
    /// <returns>The asynchronous test.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CallerCancellationStopsNewMutationsAsync(
        bool large
    )
    {
        using CancellationTokenSource caller = CreateWatchdog();
        EventWriterLeaseTestContext context = CreateContext(large);
        context.HeldBoundary = 2;
        Task<BrookPosition> append = context.Writer.AppendEventsAsync(context.Key, context.Events, null, caller.Token);
        try
        {
            await context.StorageEntered.Task.WaitAsync(caller.Token);
            await caller.CancelAsync();
            Assert.True(context.WorkToken.IsCancellationRequested);
            context.FinishStorage.TrySetResult();
            OperationCanceledException actual =
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => append.WaitAsync(CancellationToken.None));
            Assert.Equal(caller.Token, actual.CancellationToken);
        }
        finally
        {
            context.FinishStorage.TrySetResult();
            await Record.ExceptionAsync(async () => await append.WaitAsync(CancellationToken.None));
        }

        AssertNoCommitOrDeletes(context);
        context.Lease.Verify(l => l.DisposeAsync(), Times.Once);
        Assert.Equal(0, context.Clock.ActiveTimers);
    }

    /// <summary>
    ///     An arbitrary deadline task failure preserves its original identity and is joined before release.
    /// </summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task DeadlineTaskFailureRetainsFirstCauseAsync()
    {
        using CancellationTokenSource watchdog = CreateWatchdog();
        QueuedDeadlineTaskScheduler scheduler = new();
        EventWriterLeaseTestContext context = CreateContext(false, scheduler);
        context.HeldBoundary = 2;
        context.HoldRenewal = true;
        FormatException failure = new("Unknown deadline clock failure");
        Task<BrookPosition> append = context.Writer.AppendEventsAsync(
            context.Key,
            context.Events,
            null,
            watchdog.Token);
        try
        {
            await context.StorageEntered.Task.WaitAsync(watchdog.Token);
            context.Clock.Advance(TimeSpan.FromSeconds(20));
            await context.RenewalEntered.Task.WaitAsync(watchdog.Token);
            context.Clock.Advance(TimeSpan.FromSeconds(39));
            await scheduler.Queued.Task.WaitAsync(watchdog.Token);
            context.Clock.NextTimestampFailure = failure;
            scheduler.ExecuteNext();
            Assert.True(context.WorkToken.IsCancellationRequested);
            context.FinishRenewal.TrySetResult();
            context.FinishStorage.TrySetResult();
            FormatException actual = await Assert.ThrowsAsync<FormatException>(() => append.WaitAsync(watchdog.Token));
            Assert.Same(failure, actual);
        }
        finally
        {
            context.FinishStorage.TrySetResult();
            context.FinishRenewal.TrySetResult();
            while (scheduler.PendingTasks > 0)
            {
                scheduler.ExecuteNext();
            }

            await Record.ExceptionAsync(() => append.WaitAsync(CancellationToken.None));
        }

        AssertNoCommitOrDeletes(context);
        context.Lease.Verify(l => l.DisposeAsync(), Times.Once);
        Assert.Equal(0, scheduler.PendingTasks);
        Assert.Equal(0, context.Clock.ActiveTimers);
    }

    /// <summary>
    ///     Both batch paths renew while recovery, pending creation, event storage or commit is still held.
    /// </summary>
    /// <param name="large">Whether to force multiple batches.</param>
    /// <param name="boundary">The held recovery0, pending1, event2 or commit3 boundary.</param>
    /// <returns>The asynchronous test.</returns>
    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(false, 3)]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    [InlineData(true, 3)]
    public async Task HealthyRenewalCoversAwaitedStorageBoundaryAsync(
        bool large,
        int boundary
    )
    {
        using CancellationTokenSource watchdog = CreateWatchdog();
        EventWriterLeaseTestContext context = CreateContext(large);
        context.HeldBoundary = boundary;
        Task<BrookPosition> append = context.Writer.AppendEventsAsync(
            context.Key,
            context.Events,
            null,
            watchdog.Token);
        try
        {
            await context.StorageEntered.Task.WaitAsync(watchdog.Token);
            Task registration = context.Clock.NextRegistration;
            context.Clock.Advance(TimeSpan.FromSeconds(20));
            await registration.WaitAsync(watchdog.Token);
            Assert.Equal(2, context.Renewals);
            Assert.False(append.IsCompleted);
            context.FinishStorage.TrySetResult();
            Assert.Equal(2, (await append.WaitAsync(watchdog.Token)).Value);
        }
        finally
        {
            context.FinishStorage.TrySetResult();
            await append.WaitAsync(CancellationToken.None);
        }

        Assert.Equal(large ? new long[] { 1, 2 } : new long[] { 1 }, context.AppendedPositions);
        context.Repository.Verify(
            r => r.CreatePendingCursorAsync(context.Key, new(0), 2, It.IsAny<Action>(), It.IsAny<CancellationToken>()),
            Times.Once);
        context.Repository.Verify(
            r => r.CommitCursorPositionAsync(context.Key, 2, It.IsAny<Action>(), It.IsAny<CancellationToken>()),
            Times.Once);
        context.Lease.Verify(l => l.DisposeAsync(), Times.Once);
        Assert.Equal(0, context.Clock.ActiveTimers);
    }

    /// <summary>
    ///     Confirmed and unknown ownership failures stop later batches, cursor publication and compensating deletion.
    /// </summary>
    /// <param name="large">Whether to force multiple batches.</param>
    /// <param name="status">The provider failure status, with zero representing an unknown timeout.</param>
    /// <returns>The asynchronous test.</returns>
    [Theory]
    [InlineData(false, 404)]
    [InlineData(false, 409)]
    [InlineData(false, 500)]
    [InlineData(false, 0)]
    [InlineData(true, 404)]
    [InlineData(true, 409)]
    [InlineData(true, 500)]
    [InlineData(true, 0)]
    public async Task LostOrUnknownRenewalStopsNewMutationsAsync(
        bool large,
        int status
    )
    {
        using CancellationTokenSource watchdog = CreateWatchdog();
        EventWriterLeaseTestContext context = CreateContext(large);
        context.HeldBoundary = 2;
        Exception failure = status == 0
            ? new TimeoutException("Unknown renewal outcome")
            : new InvalidOperationException("Provider renewal failed", new RequestFailedException(status, "lease"));
        context.RenewalFailure = failure;
        Task<BrookPosition> append = context.Writer.AppendEventsAsync(
            context.Key,
            context.Events,
            null,
            watchdog.Token);
        try
        {
            await context.StorageEntered.Task.WaitAsync(watchdog.Token);
            TaskCompletionSource canceled = new(TaskCreationOptions.RunContinuationsAsynchronously);
            using CancellationTokenRegistration
                registration = context.WorkToken.Register(() => canceled.TrySetResult());
            context.Clock.Advance(TimeSpan.FromSeconds(20));
            await canceled.Task.WaitAsync(watchdog.Token);
            context.FinishStorage.TrySetResult();
            Exception? actual = await Record.ExceptionAsync(async () => await append.WaitAsync(watchdog.Token));
            Assert.Same(failure, actual);
        }
        finally
        {
            context.FinishStorage.TrySetResult();
            await Record.ExceptionAsync(async () => await append.WaitAsync(CancellationToken.None));
        }

        Assert.Equal(new long[] { 1 }, context.AppendedPositions);
        AssertNoCommitOrDeletes(context);
        context.Lease.Verify(l => l.DisposeAsync(), Times.Once);
        Assert.Equal(0, context.Clock.ActiveTimers);
    }

    /// <summary>
    ///     The actual renewal is joined before lock release and secondary shutdown errors preserve the primary storage error.
    /// </summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task PrimaryStorageFailureSurvivesJoinedRenewalAndReleaseFailuresAsync()
    {
        using CancellationTokenSource watchdog = CreateWatchdog();
        EventWriterLeaseTestContext context = CreateContext(false);
        context.HeldBoundary = 2;
        context.HoldRenewal = true;
        InvalidOperationException primary = new("Primary storage outcome is unknown");
        context.StorageFailure = primary;
        context.RenewalFailure = new TimeoutException("Secondary renewal failure during shutdown");
        context.ReleaseFailure = new InvalidOperationException("Secondary release failure");
        Task<BrookPosition> append = context.Writer.AppendEventsAsync(
            context.Key,
            context.Events,
            null,
            watchdog.Token);
        try
        {
            await context.StorageEntered.Task.WaitAsync(watchdog.Token);
            context.Clock.Advance(TimeSpan.FromSeconds(20));
            await context.RenewalEntered.Task.WaitAsync(watchdog.Token);
            context.FinishStorage.TrySetResult();
            await context.RenewalStopObserved.Task.WaitAsync(watchdog.Token);
            Assert.False(append.IsCompleted);
            context.Lease.Verify(l => l.DisposeAsync(), Times.Never);
            context.FinishRenewal.TrySetResult();
            InvalidOperationException actual =
                await Assert.ThrowsAsync<InvalidOperationException>(() => append.WaitAsync(CancellationToken.None));
            Assert.Same(primary, actual);
        }
        finally
        {
            context.FinishStorage.TrySetResult();
            context.FinishRenewal.TrySetResult();
            await Record.ExceptionAsync(async () => await append.WaitAsync(CancellationToken.None));
        }

        context.Lease.Verify(l => l.DisposeAsync(), Times.Once);
        AssertNoCommitOrDeletes(context);
        Assert.Equal(0, context.Clock.ActiveTimers);
    }

    /// <summary>
    ///     The writer joins a real deadline task held after its timer callback returns before releasing the lease.
    /// </summary>
    /// <returns>The asynchronous test.</returns>
    [Fact]
    public async Task QueuedDeadlineTaskIsJoinedBeforeLeaseReleaseAsync()
    {
        using CancellationTokenSource watchdog = CreateWatchdog();
        QueuedDeadlineTaskScheduler scheduler = new();
        EventWriterLeaseTestContext context = CreateContext(false, scheduler);
        context.HeldBoundary = 2;
        context.HoldRenewal = true;
        context.Clock.HoldDeadlineDisposal = true;
        CancellationToken appendCancellationToken = watchdog.Token;
        Task<BrookPosition> append = Task.Run(
            async () => await context.Writer.AppendEventsAsync(
                context.Key,
                context.Events,
                null,
                appendCancellationToken),
            TestContext.Current.CancellationToken);
        try
        {
            await context.StorageEntered.Task.WaitAsync(watchdog.Token);
            context.Clock.Advance(TimeSpan.FromSeconds(20));
            await context.RenewalEntered.Task.WaitAsync(watchdog.Token);
            context.Clock.Advance(TimeSpan.FromSeconds(39));
            await scheduler.Queued.Task.WaitAsync(watchdog.Token);
            Assert.Equal(1, scheduler.PendingTasks);
            Assert.False(context.WorkToken.IsCancellationRequested);
            context.FinishStorage.TrySetResult();
            await context.RenewalStopObserved.Task.WaitAsync(watchdog.Token);
            Assert.True(context.WorkToken.IsCancellationRequested);
            context.FinishRenewal.TrySetResult();
            await context.Clock.DeadlineDisposal.Registered.Task.WaitAsync(watchdog.Token);
            await Task.Run(context.Clock.DeadlineDisposal.Complete, TestContext.Current.CancellationToken);
            Assert.True(context.Clock.DeadlineDisposal.ContinuationReturned);
            Assert.False(append.IsCompleted);
            context.Lease.Verify(l => l.DisposeAsync(), Times.Never);
            Assert.Equal(1, scheduler.PendingTasks);
            scheduler.ExecuteNext();
            TimeoutException actual =
                await Assert.ThrowsAsync<TimeoutException>(() => append.WaitAsync(watchdog.Token));
            Assert.Contains("trusted lease window", actual.Message, StringComparison.Ordinal);
        }
        finally
        {
            context.FinishStorage.TrySetResult();
            context.FinishRenewal.TrySetResult();
            context.Clock.DeadlineDisposal.Complete();
            while (scheduler.PendingTasks > 0)
            {
                scheduler.ExecuteNext();
            }

            await Record.ExceptionAsync(() => append.WaitAsync(CancellationToken.None));
        }

        AssertNoCommitOrDeletes(context);
        context.Lease.Verify(l => l.DisposeAsync(), Times.Once);
        Assert.Equal(0, scheduler.PendingTasks);
        Assert.Equal(0, context.Clock.ActiveTimers);
    }
}