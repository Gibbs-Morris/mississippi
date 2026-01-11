using System;


namespace Mississippi.EventSourcing.Reducers.Abstractions;

/// <summary>
///     Provides a base class for reducers that transform an incoming event into a projection.
/// </summary>
/// <typeparam name="TEvent">The event type consumed by the reducer.</typeparam>
/// <typeparam name="TProjection">The projection type produced by the reducer.</typeparam>
public abstract class ReducerBase<TEvent, TProjection> : IReducer<TEvent, TProjection>
{
    /// <inheritdoc />
    public TProjection Reduce(
        TProjection state,
        TEvent eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        TProjection projection = ReduceCore(state, eventData);
        if (!typeof(TProjection).IsValueType && state is not null && ReferenceEquals(state, projection))
        {
            throw new InvalidOperationException(
                "Reducers must return a new projection instance. Use a copy/with expression instead of mutating state.");
        }

        return projection;
    }

    /// <inheritdoc />
    public bool TryReduce(
        TProjection state,
        object eventData,
        out TProjection projection
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        if (eventData is not TEvent typedEvent)
        {
            projection = default!;
            return false;
        }

        projection = Reduce(state, typedEvent);
        return true;
    }

    /// <summary>
    ///     Applies an event to the current projection to produce the next projection.
    /// </summary>
    /// <param name="state">The current projection state.</param>
    /// <param name="eventData">The event to reduce.</param>
    /// <returns>The projection produced from applying the event.</returns>
    protected abstract TProjection ReduceCore(
        TProjection state,
        TEvent eventData
    );
}