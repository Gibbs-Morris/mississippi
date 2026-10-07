using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;


namespace Mississippi.Brooks.Runtime.Storage.Cosmos.L0Tests.Locking;

/// <summary>
///     Holds one timer disposal await and explicitly resumes its registered continuation.
/// </summary>
internal sealed class HeldTimerDisposal : IValueTaskSource
{
    private readonly object state = new();

    private bool completed;

    private Action<object?>? continuation;

    private bool continuationReturned;

    private object? continuationState;

    /// <summary>
    ///     Gets a value indicating whether the actual disposal continuation has returned.
    /// </summary>
    public bool ContinuationReturned
    {
        get
        {
            lock (state)
            {
                return continuationReturned;
            }
        }
    }

    /// <summary>
    ///     Gets the barrier proving that the actual disposal await registered its continuation.
    /// </summary>
    public TaskCompletionSource Registered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    ///     Rejects any version other than this single-use source's initial version.
    /// </summary>
    /// <param name="token">The value-task source version.</param>
    private static void ValidateToken(
        short token
    )
    {
        if (token != 0)
        {
            throw new InvalidOperationException("Unexpected timer disposal version.");
        }
    }

    /// <summary>
    ///     Completes disposal and invokes its actual registered continuation before returning.
    /// </summary>
    public void Complete()
    {
        Action<object?>? resume;
        object? resumeState;
        lock (state)
        {
            if (completed)
            {
                return;
            }

            completed = true;
            resume = continuation;
            resumeState = continuationState;
        }

        if (resume is not null)
        {
            resume(resumeState);
            lock (state)
            {
                continuationReturned = true;
            }
        }
    }

    /// <inheritdoc />
    public void GetResult(
        short token
    )
    {
        ValidateToken(token);
        lock (state)
        {
            if (!completed)
            {
                throw new InvalidOperationException("Timer disposal has not been released.");
            }
        }
    }

    /// <inheritdoc />
    public ValueTaskSourceStatus GetStatus(
        short token
    )
    {
        ValidateToken(token);
        lock (state)
        {
            return completed ? ValueTaskSourceStatus.Succeeded : ValueTaskSourceStatus.Pending;
        }
    }

    /// <inheritdoc />
    public void OnCompleted(
        Action<object?> continuation,
        object? state,
        short token,
        ValueTaskSourceOnCompletedFlags flags
    )
    {
        ValidateToken(token);
        if (flags != ValueTaskSourceOnCompletedFlags.None)
        {
            throw new InvalidOperationException("The controlled disposal must use its explicit context-free await.");
        }

        bool alreadyCompleted;
        lock (this.state)
        {
            if (this.continuation is not null)
            {
                throw new InvalidOperationException("The controlled timer disposal was awaited twice.");
            }

            this.continuation = continuation;
            continuationState = state;
            alreadyCompleted = completed;
        }

        Registered.TrySetResult();
        if (alreadyCompleted)
        {
            continuation(state);
            lock (this.state)
            {
                continuationReturned = true;
            }
        }
    }

    /// <summary>
    ///     Creates the single disposal await owned by the timer.
    /// </summary>
    /// <returns>The held disposal await.</returns>
    public ValueTask WaitAsync() => new(this, 0);
}