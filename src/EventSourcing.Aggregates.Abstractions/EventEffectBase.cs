using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;


namespace Mississippi.EventSourcing.Aggregates.Abstractions;

/// <summary>
///     Base class for event effects that handle a specific event type.
/// </summary>
/// <typeparam name="TEvent">The event type this effect handles.</typeparam>
/// <typeparam name="TAggregate">The aggregate state type this effect is registered for.</typeparam>
/// <remarks>
///     <para>
///         Inherit from this class to create strongly-typed event effects.
///         The base class handles type checking in <see cref="CanHandle" /> and
///         dispatches to the typed <see cref="HandleAsync(TEvent, TAggregate, string, long, CancellationToken)" />
///         method.
///     </para>
///     <para>
///         Effects should be stateless and registered as transient services.
///         They are discovered by the source generator in the <c>Effects</c> sub-namespace.
///     </para>
/// </remarks>
public abstract class EventEffectBase<TEvent, TAggregate> : IEventEffect<TAggregate>
{
    /// <inheritdoc />
    public bool CanHandle(
        object eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        return eventData is TEvent;
    }

    /// <inheritdoc />
    public IAsyncEnumerable<object> HandleAsync(
        object eventData,
        TAggregate currentState,
        string brookKey,
        long eventPosition,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        if (eventData is TEvent typedEvent)
        {
            return HandleAsync(typedEvent, currentState, brookKey, eventPosition, cancellationToken);
        }

        return AsyncEnumerable.Empty<object>();
    }

    /// <summary>
    ///     Handles the event and optionally yields additional events.
    /// </summary>
    /// <param name="eventData">The event that was persisted.</param>
    /// <param name="currentState">The current aggregate state after the event was applied.</param>
    /// <param name="brookKey">The brook key identifying the aggregate instance.</param>
    /// <param name="eventPosition">The position of the event in the brook.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    ///     An async enumerable of additional events to persist.
    /// </returns>
    public abstract IAsyncEnumerable<object> HandleAsync(
        TEvent eventData,
        TAggregate currentState,
        string brookKey,
        long eventPosition,
        CancellationToken cancellationToken
    );
}