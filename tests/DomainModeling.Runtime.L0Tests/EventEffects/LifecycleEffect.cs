using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.DomainModeling.Abstractions;


namespace Mississippi.DomainModeling.Runtime.L0Tests.EventEffects;

/// <summary>
///     Legal typed effect with an explicit enumerable and enumerator lifecycle.
/// </summary>
internal sealed class LifecycleEffect
    : EventEffectBase<EffectLifecycleEvent, EffectLifecycleAggregate>,
      IAsyncEnumerable<object>
{
    private int position = -1;

    /// <summary>Gets the number of enumerator acquisition attempts.</summary>
    internal int AcquisitionCount { get; private set; }

    /// <summary>Gets a value indicating whether a failed ValueTask carries the exception.</summary>
    internal bool AsynchronousFault { get; init; }

    /// <summary>Gets the item at the current enumerator position.</summary>
    internal object Current
    {
        get
        {
            CurrentReadCount++;
            if ((position == 1) && (FailurePoint == EffectLifecyclePoint.Current))
            {
                throw Failure!;
            }

            return position == 0 ? FirstEvent : SecondEvent;
        }
    }

    /// <summary>Gets the number of current-item reads.</summary>
    internal int CurrentReadCount { get; private set; }

    /// <summary>Gets the number of enumerator disposal attempts.</summary>
    internal int DisposalCount { get; private set; }

    /// <summary>Gets the separate disposal failure, if configured.</summary>
    internal Exception? DisposalFailure { get; init; }

    /// <summary>Gets the token passed to enumerator acquisition.</summary>
    internal CancellationToken EnumeratorToken { get; private set; }

    /// <summary>Gets the failure raised by the selected operation.</summary>
    internal Exception? Failure { get; init; }

    /// <summary>Gets the operation selected to fail.</summary>
    internal EffectLifecyclePoint FailurePoint { get; init; }

    /// <summary>Gets the first item produced by the enumerator.</summary>
    internal object FirstEvent { get; } = new();

    /// <summary>Gets the number of effect invocation attempts.</summary>
    internal int HandleCount { get; private set; }

    /// <summary>Gets the token passed to effect invocation.</summary>
    internal CancellationToken HandleToken { get; private set; }

    /// <summary>Gets the number of enumerator advances.</summary>
    internal int MoveNextCount { get; private set; }

    /// <summary>Gets the second item produced by the enumerator.</summary>
    internal object SecondEvent { get; } = new();

    /// <inheritdoc />
    public IAsyncEnumerator<object> GetAsyncEnumerator(
        CancellationToken cancellationToken = default
    )
    {
        AcquisitionCount++;
        EnumeratorToken = cancellationToken;
        if (FailurePoint == EffectLifecyclePoint.Acquire)
        {
            throw Failure!;
        }

        return new LifecycleEnumerator(this);
    }

    /// <inheritdoc />
    public override IAsyncEnumerable<object> HandleAsync(
        EffectLifecycleEvent eventData,
        EffectLifecycleAggregate currentState,
        string brookKey,
        long eventPosition,
        CancellationToken cancellationToken
    )
    {
        HandleCount++;
        HandleToken = cancellationToken;
        if (FailurePoint == EffectLifecyclePoint.Handle)
        {
            throw Failure!;
        }

        return this;
    }

    /// <summary>Releases the acquired enumerator once.</summary>
    /// <returns>The disposal operation.</returns>
    internal ValueTask DisposeAsync()
    {
        DisposalCount++;
        Exception? failure = FailurePoint == EffectLifecyclePoint.Dispose ? Failure : DisposalFailure;
        if (failure is null)
        {
            return ValueTask.CompletedTask;
        }

        if (!AsynchronousFault)
        {
            throw failure;
        }

        return ValueTask.FromException(failure);
    }

    /// <summary>Advances the enumerator to its next item.</summary>
    /// <returns>Whether an item is available.</returns>
    internal ValueTask<bool> MoveNextAsync()
    {
        MoveNextCount++;
        position++;
        if ((position == 1) && (FailurePoint == EffectLifecyclePoint.MoveNext))
        {
            if (!AsynchronousFault)
            {
                throw Failure!;
            }

            return ValueTask.FromException<bool>(Failure!);
        }

        return ValueTask.FromResult(position < 2);
    }
}