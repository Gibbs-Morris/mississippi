using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Time.Testing;


namespace Mississippi.Brooks.Runtime.Storage.Cosmos.L0Tests.Locking;

/// <summary>
///     Records explicit timer registration and deadline callback barriers around a deterministic clock.
/// </summary>
internal sealed class LeaseTestTimeProvider : TimeProvider
{
    private readonly object state = new();

    private int activeTimers;

    private TaskCompletionSource nextRegistration = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private Exception? nextTimestampFailure;

    /// <summary>
    ///     Initializes a new instance of the <see cref="LeaseTestTimeProvider" /> class.
    /// </summary>
    /// <param name="initialTime">The fixed initial UTC time.</param>
    public LeaseTestTimeProvider(
        DateTimeOffset initialTime
    ) =>
        Clock = new(initialTime);

    /// <summary>
    ///     Gets the number of timers whose owned disposal has not completed.
    /// </summary>
    public int ActiveTimers => Volatile.Read(ref activeTimers);

    /// <summary>
    ///     Gets the controlled disposal completion and its actual continuation-registration barrier.
    /// </summary>
    public HeldTimerDisposal DeadlineDisposal { get; } = new();

    /// <summary>
    ///     Gets the barrier signaling asynchronous deadline disposal.
    /// </summary>
    public TaskCompletionSource DeadlineDisposalEntered { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    ///     Gets the barrier signaling a held deadline callback.
    /// </summary>
    public TaskCompletionSource DeadlineEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    ///     Gets the explicit release for a held deadline callback.
    /// </summary>
    public TaskCompletionSource DeadlineRelease { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    ///     Gets or sets a value indicating whether a delivered deadline callback waits on its release barrier.
    /// </summary>
    public bool HoldDeadline { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether deadline disposal waits for explicit completion.
    /// </summary>
    public bool HoldDeadlineDisposal { get; set; }

    /// <inheritdoc />
    public override TimeZoneInfo LocalTimeZone => Clock.LocalTimeZone;

    /// <summary>
    ///     Gets the next actual timer-registration barrier.
    /// </summary>
    public Task NextRegistration
    {
        get
        {
            lock (state)
            {
                return nextRegistration.Task.WaitAsync(CancellationToken.None);
            }
        }
    }

    /// <summary>
    ///     Gets or sets the single failure consumed by the next monotonic timestamp read.
    /// </summary>
    public Exception? NextTimestampFailure
    {
        get => Volatile.Read(ref nextTimestampFailure);
        set => Interlocked.Exchange(ref nextTimestampFailure, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether automatic deadline delivery is suppressed.
    /// </summary>
    public bool SuppressDeadline { get; set; }

    /// <inheritdoc />
    public override long TimestampFrequency => Clock.TimestampFrequency;

    /// <summary>
    ///     Gets or sets a UTC offset that leaves the monotonic clock unchanged.
    /// </summary>
    public TimeSpan UtcOffset { get; set; }

    private FakeTimeProvider Clock { get; }

    private TrackedTimer? Deadline { get; set; }

    /// <summary>
    ///     Advances the independent clock; callers wait on the specific operation or registration barrier.
    /// </summary>
    /// <param name="amount">The virtual elapsed duration.</param>
    public void Advance(
        TimeSpan amount
    ) =>
        Clock.Advance(amount);

    /// <inheritdoc />
    public override ITimer CreateTimer(
        TimerCallback callback,
        object? state,
        TimeSpan dueTime,
        TimeSpan period
    )
    {
        TrackedTimer timer = new(this, callback, state, dueTime, period);
        TaskCompletionSource registered;
        lock (this.state)
        {
            if (dueTime == Timeout.InfiniteTimeSpan)
            {
                Deadline = timer;
            }

            registered = nextRegistration;
            nextRegistration = new(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        Interlocked.Increment(ref activeTimers);
        registered.TrySetResult();
        return timer;
    }

    /// <summary>
    ///     Delivers an explicit stale deadline callback to verify the owner's current deadline check.
    /// </summary>
    public void DeliverDeadline() => Deadline?.Deliver();

    /// <inheritdoc />
    public override long GetTimestamp()
    {
        Exception? failure = Interlocked.Exchange(ref nextTimestampFailure, null);
        if (failure is not null)
        {
            throw failure;
        }

        return Clock.GetTimestamp();
    }

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow() => Clock.GetUtcNow() + UtcOffset;

    /// <summary>
    ///     Owns one fake timer and any explicitly held callback dispatched by it.
    /// </summary>
    private sealed class TrackedTimer : ITimer
    {
        private Task callbackCompletion = Task.CompletedTask;

        private int disposed;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TrackedTimer" /> class.
        /// </summary>
        /// <param name="owner">The clock recording ownership.</param>
        /// <param name="callback">The actual callback.</param>
        /// <param name="callbackState">The callback state.</param>
        /// <param name="dueTime">The initial due time.</param>
        /// <param name="period">The timer period.</param>
        public TrackedTimer(
            LeaseTestTimeProvider owner,
            TimerCallback callback,
            object? callbackState,
            TimeSpan dueTime,
            TimeSpan period
        )
        {
            Owner = owner;
            Callback = callback;
            CallbackState = callbackState;
            IsDeadline = dueTime == Timeout.InfiniteTimeSpan;
            Inner = owner.Clock.CreateTimer(static timer => ((TrackedTimer)timer!).Deliver(), this, dueTime, period);
        }

        private TimerCallback Callback { get; }

        private object? CallbackState { get; }

        private ITimer Inner { get; }

        private bool IsDeadline { get; }

        private LeaseTestTimeProvider Owner { get; }

        /// <inheritdoc />
        public bool Change(
            TimeSpan dueTime,
            TimeSpan period
        ) =>
            Inner.Change(dueTime, period);

        /// <summary>
        ///     Delivers or explicitly holds the actual callback.
        /// </summary>
        public void Deliver()
        {
            if (IsDeadline && Owner.SuppressDeadline)
            {
                return;
            }

            if (IsDeadline && Owner.HoldDeadline)
            {
                callbackCompletion = DeliverHeldAsync();
            }
            else
            {
                Callback(CallbackState);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) == 0)
            {
                Inner.Dispose();
                Interlocked.Decrement(ref Owner.activeTimers);
            }
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref disposed, 1) == 0)
            {
                if (IsDeadline)
                {
                    Owner.DeadlineDisposalEntered.TrySetResult();
                }

                await Inner.DisposeAsync().ConfigureAwait(false);
                await callbackCompletion.WaitAsync(CancellationToken.None).ConfigureAwait(false);
                if (IsDeadline && Owner.HoldDeadlineDisposal)
                {
                    await Owner.DeadlineDisposal.WaitAsync().ConfigureAwait(false);
                }

                Interlocked.Decrement(ref Owner.activeTimers);
            }
        }

        /// <summary>
        ///     Holds deadline delivery until the test proves disposal is awaiting it.
        /// </summary>
        /// <returns>The owned callback completion.</returns>
        private async Task DeliverHeldAsync()
        {
            Owner.DeadlineEntered.TrySetResult();
            await Owner.DeadlineRelease.Task.WaitAsync(CancellationToken.None);
            Callback(CallbackState);
        }
    }
}