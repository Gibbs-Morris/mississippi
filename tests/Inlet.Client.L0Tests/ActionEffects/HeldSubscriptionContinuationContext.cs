using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.Inlet.Client.L0Tests.ActionEffects;

/// <summary>Holds captured continuations until a subscription race reaches its release boundary.</summary>
internal sealed class HeldSubscriptionContinuationContext : SynchronizationContext
{
    private readonly Queue<(SendOrPostCallback Callback, object? State)> callbacks = new();

    private readonly object gate = new();

    private readonly TaskCompletionSource posted = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private bool resumed;

    /// <inheritdoc />
    public override void Post(
        SendOrPostCallback d,
        object? state
    )
    {
        lock (gate)
        {
            if (!resumed)
            {
                callbacks.Enqueue((d, state));
                posted.TrySetResult();
                return;
            }
        }

        ThreadPool.QueueUserWorkItem(_ => d(state));
    }

    /// <summary>Releases all captured continuations and allows later continuations to run.</summary>
    public void Resume()
    {
        (SendOrPostCallback Callback, object? State)[] pending;
        lock (gate)
        {
            resumed = true;
            pending = callbacks.ToArray();
            callbacks.Clear();
        }

        foreach ((SendOrPostCallback callback, object? state) in pending)
        {
            ThreadPool.QueueUserWorkItem(_ => callback(state));
        }
    }

    /// <summary>Starts work while its awaits capture this held context.</summary>
    /// <typeparam name="T">The result returned when starting work.</typeparam>
    /// <param name="start">The work to start.</param>
    /// <returns>The started work.</returns>
    public T Run<T>(
        Func<T> start
    )
    {
        SynchronizationContext? previous = Current;
        SetSynchronizationContext(this);
        try
        {
            return start();
        }
        finally
        {
            SetSynchronizationContext(previous);
        }
    }

    /// <summary>Waits until an asynchronous completion has posted a held continuation.</summary>
    /// <param name="cancellationToken">The test cancellation token.</param>
    /// <returns>A task completing when the handoff is held.</returns>
    public Task WaitForContinuationAsync(
        CancellationToken cancellationToken
    ) =>
        posted.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
}