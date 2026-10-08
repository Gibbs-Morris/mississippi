using System;
using System.Threading;


namespace MississippiTests.Inlet.Client.L1Tests.ActionEffects;

/// <summary>
///     Reenters readiness at the first posted continuation before queuing that continuation.
/// </summary>
internal sealed class ReadinessCallbackSynchronizationContext : SynchronizationContext
{
    private readonly Action firstPost;

    private int posted;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ReadinessCallbackSynchronizationContext" /> class.
    /// </summary>
    /// <param name="firstPost">The readiness callback to run before the first continuation is posted.</param>
    internal ReadinessCallbackSynchronizationContext(
        Action firstPost
    )
    {
        ArgumentNullException.ThrowIfNull(firstPost);
        this.firstPost = firstPost;
    }

    /// <inheritdoc />
    public override void Post(
        SendOrPostCallback d,
        object? state
    )
    {
        if (Interlocked.Exchange(ref posted, 1) == 0)
        {
            firstPost();
        }

        ThreadPool.QueueUserWorkItem(_ => d(state));
    }
}