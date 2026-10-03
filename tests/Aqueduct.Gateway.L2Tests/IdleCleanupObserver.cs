using System.Collections.Concurrent;

using Orleans.Runtime;


namespace Mississippi.Aqueduct.Gateway.L2Tests;

/// <summary>
///     Observes completed grain ownership queries without calling or refreshing the tested activations.
/// </summary>
internal sealed class IdleCleanupObserver : IIncomingGrainCallFilter
{
    private ConcurrentDictionary<ObservedCall, TaskCompletionSource<bool>> Calls { get; } = new();

    /// <inheritdoc />
    public async Task Invoke(
        IIncomingGrainCallContext context
    )
    {
        await context.Invoke();
        if (context.SourceId is { } source)
        {
            Calls.GetOrAdd(
                    new(source, context.TargetId, context.MethodName),
                    static _ => new(TaskCreationOptions.RunContinuationsAsynchronously))
                .TrySetResult(true);
        }
    }

    /// <summary>
    ///     Waits for a successful call from the specified owner to its liveness dependency.
    /// </summary>
    /// <param name="source">The activation whose cleanup timer issues the query.</param>
    /// <param name="target">The queried activation.</param>
    /// <param name="method">The ownership query method.</param>
    /// <returns>A task completed after the matching call returns successfully.</returns>
    public Task WaitForCallAsync(
        GrainId source,
        GrainId target,
        string method
    ) =>
        Calls.GetOrAdd(new(source, target, method), static _ => new(TaskCreationOptions.RunContinuationsAsynchronously))
            .Task;

    private readonly record struct ObservedCall(GrainId Source, GrainId Target, string Method);
}