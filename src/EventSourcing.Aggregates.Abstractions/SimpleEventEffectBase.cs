using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.EventSourcing.Aggregates.Abstractions;

/// <summary>
///     Base class for simple event effects that perform side operations without yielding events.
/// </summary>
/// <typeparam name="TEvent">The event type this effect handles.</typeparam>
/// <typeparam name="TAggregate">The aggregate state type this effect is registered for.</typeparam>
/// <remarks>
///     <para>
///         Use this base class when your effect performs a side operation (logging, notification,
///         external API call) but doesn't need to yield additional events to the aggregate.
///     </para>
///     <para>
///         This simplifies the implementation by requiring only a <see cref="Task" /> return type
///         instead of <see cref="IAsyncEnumerable{T}" />.
///     </para>
/// </remarks>
public abstract class SimpleEventEffectBase<TEvent, TAggregate> : EventEffectBase<TEvent, TAggregate>
{
    /// <inheritdoc />
    public sealed override IAsyncEnumerable<object> HandleAsync(
        TEvent eventData,
        TAggregate currentState,
        string brookKey,
        long eventPosition,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        return HandleAsyncCoreAsync(eventData, currentState, brookKey, eventPosition, cancellationToken);
    }

    /// <summary>
    ///     Handles the event without yielding additional events.
    /// </summary>
    /// <param name="eventData">The event that was persisted.</param>
    /// <param name="currentState">The current aggregate state after the event was applied.</param>
    /// <param name="brookKey">The brook key identifying the aggregate instance.</param>
    /// <param name="eventPosition">The position of the event in the brook.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected abstract Task HandleSimpleAsync(
        TEvent eventData,
        TAggregate currentState,
        string brookKey,
        long eventPosition,
        CancellationToken cancellationToken
    );

    private async IAsyncEnumerable<object> HandleAsyncCoreAsync(
        TEvent eventData,
        TAggregate currentState,
        string brookKey,
        long eventPosition,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        await HandleSimpleAsync(eventData, currentState, brookKey, eventPosition, cancellationToken);
        yield break;
    }
}