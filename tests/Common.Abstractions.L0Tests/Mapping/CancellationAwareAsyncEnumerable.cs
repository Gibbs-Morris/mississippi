using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace MississippiTests.Common.Abstractions.L0Tests.Mapping;

/// <summary>
///     Provides a pending source move that observes only the enumeration token.
/// </summary>
internal sealed class CancellationAwareAsyncEnumerable
    : IAsyncEnumerable<int>,
      IAsyncEnumerator<int>
{
    private CancellationTokenRegistration cancellationRegistration;

    /// <inheritdoc />
    public int Current => 1;

    /// <summary>
    ///     Gets the token supplied by the source consumer.
    /// </summary>
    public CancellationToken EnumerationToken { get; private set; }

    /// <summary>
    ///     Gets a value indicating whether the source enumerator was disposed.
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    ///     Gets a value indicating whether the source enumerator was created.
    /// </summary>
    public bool IsEnumeratorCreated { get; private set; }

    /// <summary>
    ///     Gets the signal that a source move has started.
    /// </summary>
    public Task Started => StartedCompletion.Task;

    private TaskCompletionSource<bool> MoveCompletion { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private TaskCompletionSource StartedCompletion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        cancellationRegistration.Dispose();
        IsDisposed = true;
        Release();
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public IAsyncEnumerator<int> GetAsyncEnumerator(
        CancellationToken cancellationToken = default
    )
    {
        EnumerationToken = cancellationToken;
        IsEnumeratorCreated = true;
        cancellationRegistration = cancellationToken.Register(() => MoveCompletion.TrySetCanceled(cancellationToken));
        return this;
    }

    /// <inheritdoc />
    public ValueTask<bool> MoveNextAsync()
    {
        StartedCompletion.TrySetResult();
        return new(MoveCompletion.Task);
    }

    /// <summary>
    ///     Releases a pending move when an assertion fails before cancellation reaches the source.
    /// </summary>
    public void Release() => MoveCompletion.TrySetResult(false);
}