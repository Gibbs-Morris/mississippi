using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;


namespace Mississippi.Brooks.Runtime.Storage.Cosmos.Locking;

/// <summary>
///     Owns serialized renewal and a conservative ownership deadline for one append.
/// </summary>
internal sealed class AppendLeaseLifetime : IAsyncDisposable
{
    private readonly List<Task> deadlineTasks = new();

    private readonly TaskCompletionSource initialRenewal = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly TaskCompletionSource renewalStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly object state = new();

    private readonly TaskCompletionSource stopStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private bool active;

    private Exception? cleanupFailure;

    private bool deadlineArmed;

    private Task? disposal;

    private Exception? failure;

    private long lastConfirmedRequest;

    private Task? renewals;

    private bool started;

    private bool stopping;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AppendLeaseLifetime" /> class.
    /// </summary>
    /// <param name="distributedLock">The acquired append lock.</param>
    /// <param name="options">The configured finite lease duration and renewal threshold.</param>
    /// <param name="timeProvider">The clock shared with the distributed lock.</param>
    /// <param name="callerCancellationToken">Cancellation requested by the caller.</param>
    /// <param name="deadlineScheduler">The internal deadline scheduler, or the default thread-pool scheduler.</param>
    public AppendLeaseLifetime(
        IDistributedLock distributedLock,
        IOptions<BrookStorageOptions> options,
        TimeProvider timeProvider,
        CancellationToken callerCancellationToken,
        TaskScheduler? deadlineScheduler = null
    )
    {
        ArgumentNullException.ThrowIfNull(distributedLock);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ValidateOptions(options.Value);
        DistributedLock = distributedLock;
        TimeProvider = timeProvider;
        DeadlineScheduler = deadlineScheduler ?? TaskScheduler.Default;
        CallerCancellationToken = callerCancellationToken;
        double duration = options.Value.LeaseDurationSeconds;
        double threshold = options.Value.LeaseRenewalThresholdSeconds;
        RenewalDelay = TimeSpan.FromSeconds(Math.Min(duration / 3, Math.Max(1, duration - threshold - 1)));
        TrustedWindow = TimeSpan.FromSeconds(duration - 1);
        WorkCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            callerCancellationToken,
            LeaseLossCancellation.Token);
        RenewalCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            WorkCancellation.Token,
            StopCancellation.Token);
        CancellationToken = WorkCancellation.Token;
        DeadlineTimer = timeProvider.CreateTimer(
            static owner => ((AppendLeaseLifetime)owner!).OnDeadline(),
            this,
            Timeout.InfiniteTimeSpan,
            Timeout.InfiniteTimeSpan);
    }

    /// <summary>
    ///     Gets a value indicating whether a known append failure may still be rolled back under this lease.
    /// </summary>
    public bool CanRollback
    {
        get
        {
            CheckDeadline(false);
            lock (state)
            {
                return active && !stopping && failure is null && !CancellationToken.IsCancellationRequested;
            }
        }
    }

    /// <summary>
    ///     Gets cancellation shared by the append's storage operations.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    ///     Gets the first observed lease failure, if any.
    /// </summary>
    public Exception? Failure
    {
        get
        {
            lock (state)
            {
                return failure;
            }
        }
    }

    private CancellationToken CallerCancellationToken { get; }

    private TaskScheduler DeadlineScheduler { get; }

    private ITimer DeadlineTimer { get; }

    private IDistributedLock DistributedLock { get; }

    private CancellationTokenSource LeaseLossCancellation { get; } = new();

    private CancellationTokenSource RenewalCancellation { get; }

    private TimeSpan RenewalDelay { get; }

    private CancellationTokenSource StopCancellation { get; } = new();

    private TimeProvider TimeProvider { get; }

    private TimeSpan TrustedWindow { get; }

    private CancellationTokenSource WorkCancellation { get; }

    /// <summary>
    ///     Rejects invalid lease options before acquisition or storage dispatch.
    /// </summary>
    /// <param name="options">The append lease options.</param>
    public static void ValidateOptions(
        BrookStorageOptions options
    )
    {
        ArgumentNullException.ThrowIfNull(options);
        if ((options.LeaseDurationSeconds < 15) || (options.LeaseDurationSeconds > 60))
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "Append lease duration must be between 15 and 60 seconds.");
        }

        if ((options.LeaseRenewalThresholdSeconds < 0) ||
            (options.LeaseRenewalThresholdSeconds >= options.LeaseDurationSeconds))
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "Append renewal threshold must be nonnegative and shorter than the lease.");
        }
    }

    /// <summary>
    ///     Stops and joins all owned renewal and timer work before the lock is released.
    /// </summary>
    /// <returns>The shared asynchronous disposal operation.</returns>
    public ValueTask DisposeAsync()
    {
        CheckDeadline(false);
        Task completion;
        lock (state)
        {
            stopping = true;
            disposal ??= DisposeCoreAsync();
            completion = disposal;
        }

        stopStarted.TrySetResult();
        return new(completion);
    }

    /// <summary>
    ///     Confirms an actual initial renewal before permitting storage work.
    /// </summary>
    /// <returns>A task completing after the initial service renewal.</returns>
    public async Task StartAsync()
    {
        lock (state)
        {
            if (started || stopping)
            {
                throw new InvalidOperationException("The append lease lifetime cannot be started again.");
            }

            started = true;
            renewals = RunRenewalsAsync();
        }

        renewalStarted.TrySetResult();
        await initialRenewal.Task.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        ThrowIfFailed();
    }

    /// <summary>
    ///     Checks ownership and caller cancellation at a storage or success boundary.
    /// </summary>
    public void ThrowIfFailed()
    {
        CheckDeadline(false);
        Exception? observed = Failure;
        if (observed is not null)
        {
            ExceptionDispatchInfo.Capture(observed).Throw();
        }

        CallerCancellationToken.ThrowIfCancellationRequested();
        CancellationToken.ThrowIfCancellationRequested();
    }

    /// <summary>
    ///     Cancels outside the state guard and retains any callback failure for joined cleanup.
    /// </summary>
    /// <param name="source">The owned cancellation source.</param>
    private void CancelOwned(
        CancellationTokenSource source
    )
    {
        try
        {
            source.Cancel();
        }
        catch (AggregateException exception)
        {
            RecordCleanupFailure(exception);
        }
    }

    /// <summary>
    ///     Observes expiry independently of timer delivery and handles a stale timer callback.
    /// </summary>
    /// <param name="rearm">Whether a still-current deadline should be rearmed.</param>
    private void CheckDeadline(
        bool rearm
    )
    {
        bool cancel = false;
        lock (state)
        {
            if (!deadlineArmed || stopping || failure is not null)
            {
                return;
            }

            TimeSpan remaining = TrustedWindow - TimeProvider.GetElapsedTime(lastConfirmedRequest);
            if (remaining <= TimeSpan.Zero)
            {
                failure = new TimeoutException("Brook lock renewal was not confirmed within its trusted lease window.");
                cancel = true;
            }
            else if (rearm)
            {
                DeadlineTimer.Change(remaining, Timeout.InfiniteTimeSpan);
            }
        }

        if (cancel)
        {
            CancelOwned(LeaseLossCancellation);
        }
    }

    /// <summary>
    ///     Joins the actual renewal task and asynchronously disposes timer callbacks before token sources.
    /// </summary>
    /// <returns>A task representing the owned shutdown.</returns>
    private async Task DisposeCoreAsync()
    {
        await stopStarted.Task.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        CancelOwned(StopCancellation);
        try
        {
            try
            {
                if (renewals is not null)
                {
                    await renewals.WaitAsync(CancellationToken.None).ConfigureAwait(false);
                }
            }
            finally
            {
                try
                {
                    await DisposeDeadlineTimerAsync().ConfigureAwait(false);
                }
                finally
                {
                    try
                    {
                        await JoinDeadlineTasksAsync().ConfigureAwait(false);
                    }
                    finally
                    {
                        RenewalCancellation.Dispose();
                        WorkCancellation.Dispose();
                        StopCancellation.Dispose();
                        LeaseLossCancellation.Dispose();
                    }
                }
            }
        }
        catch (Exception exception)
        {
            Exception? first;
            lock (state)
            {
                first = failure ?? cleanupFailure;
            }

            if (first is not null && !ReferenceEquals(first, exception))
            {
                ExceptionDispatchInfo.Capture(first).Throw();
            }

            throw;
        }

        Exception? observed;
        lock (state)
        {
            observed = failure ?? cleanupFailure;
        }

        if (observed is not null)
        {
            ExceptionDispatchInfo.Capture(observed).Throw();
        }
    }

    /// <summary>
    ///     Asynchronously disposes the timer while preserving its first cleanup failure.
    /// </summary>
    /// <returns>The actual timer disposal operation.</returns>
    private async Task DisposeDeadlineTimerAsync()
    {
        try
        {
            await DeadlineTimer.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            RecordCleanupFailure(exception);
            throw;
        }
    }

    /// <summary>
    ///     Freezes and joins every dispatched deadline task after timer disposal.
    /// </summary>
    /// <returns>The completion of all actual dispatched tasks.</returns>
    private async Task JoinDeadlineTasksAsync()
    {
        Task[] tasks;
        lock (state)
        {
            tasks = deadlineTasks.ToArray();
        }

        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            RecordCleanupFailure(exception);
            throw;
        }
    }

    /// <summary>
    ///     Registers every real dispatched task before the timer callback returns.
    /// </summary>
    private void OnDeadline()
    {
        lock (state)
        {
            if (stopping)
            {
                return;
            }

            Task dispatched = Task.Factory.StartNew(
                RunDeadlineCallback,
                CancellationToken.None,
                TaskCreationOptions.DenyChildAttach,
                DeadlineScheduler);
            deadlineTasks.Add(dispatched);
        }
    }

    /// <summary>
    ///     Publishes the first lease failure before canceling linked operations outside the guard.
    /// </summary>
    /// <param name="exception">The failure that makes ownership uncertain.</param>
    private void PublishLoss(
        Exception exception
    )
    {
        bool cancel = false;
        lock (state)
        {
            if (!stopping && failure is null)
            {
                failure = exception;
                cancel = true;
            }
        }

        if (cancel)
        {
            CancelOwned(LeaseLossCancellation);
        }
    }

    /// <summary>
    ///     Retains the first secondary error without replacing a lease failure.
    /// </summary>
    /// <param name="exception">The observed shutdown or callback error.</param>
    private void RecordCleanupFailure(
        Exception exception
    )
    {
        lock (state)
        {
            cleanupFailure ??= exception;
        }
    }

    /// <summary>
    ///     Renews through the service and confirms only a still-valid request-start window.
    /// </summary>
    /// <returns>A task representing the actual renewal request.</returns>
    private async Task RenewOnceAsync()
    {
        ThrowIfFailed();
        RenewalCancellation.Token.ThrowIfCancellationRequested();
        long requestStart = TimeProvider.GetTimestamp();
        lock (state)
        {
            if (!deadlineArmed)
            {
                lastConfirmedRequest = requestStart;
                deadlineArmed = true;
                DeadlineTimer.Change(TrustedWindow, Timeout.InfiniteTimeSpan);
            }
        }

        await DistributedLock.RenewAsync(true, RenewalCancellation.Token).ConfigureAwait(false);
        ThrowIfFailed();
        RenewalCancellation.Token.ThrowIfCancellationRequested();
        bool expired = false;
        lock (state)
        {
            if (!stopping && failure is null)
            {
                TimeSpan remaining = TrustedWindow - TimeProvider.GetElapsedTime(requestStart);
                if (remaining <= TimeSpan.Zero)
                {
                    expired = true;
                }
                else
                {
                    lastConfirmedRequest = requestStart;
                    active = true;
                    DeadlineTimer.Change(remaining, Timeout.InfiniteTimeSpan);
                }
            }
        }

        if (expired)
        {
            PublishLoss(new TimeoutException("Brook lock renewal completed after its trusted lease window."));
        }

        RenewalCancellation.Token.ThrowIfCancellationRequested();
        ThrowIfFailed();
    }

    /// <summary>
    ///     Publishes any deadline task failure before retaining it on the owned task.
    /// </summary>
    private void RunDeadlineCallback()
    {
        try
        {
            CheckDeadline(true);
        }
        catch (Exception exception)
        {
            PublishLoss(exception);
            RecordCleanupFailure(exception);
            throw;
        }
    }

    /// <summary>
    ///     Owns the initial confirmation and every subsequent renewal until joined shutdown.
    /// </summary>
    /// <returns>The single owned renewal task.</returns>
    private async Task RunRenewalsAsync()
    {
        await renewalStarted.Task.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        try
        {
            await RenewOnceAsync().ConfigureAwait(false);
            Task delay = Task.Delay(RenewalDelay, TimeProvider, RenewalCancellation.Token);
            initialRenewal.TrySetResult();
            while (!RenewalCancellation.IsCancellationRequested)
            {
                await delay.ConfigureAwait(false);
                await RenewOnceAsync().ConfigureAwait(false);
                delay = Task.Delay(RenewalDelay, TimeProvider, RenewalCancellation.Token);
            }

            RenewalCancellation.Token.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException) when (RenewalCancellation.IsCancellationRequested)
        {
            Exception? observed = Failure;
            if (observed is not null)
            {
                initialRenewal.TrySetException(observed);
            }
            else
            {
                initialRenewal.TrySetCanceled(
                    CallerCancellationToken.IsCancellationRequested ? CallerCancellationToken : StopCancellation.Token);
            }
        }
        catch (Exception exception)
        {
            PublishLoss(exception);
            RecordCleanupFailure(exception);
            initialRenewal.TrySetException(Failure ?? exception);
            throw;
        }
    }
}