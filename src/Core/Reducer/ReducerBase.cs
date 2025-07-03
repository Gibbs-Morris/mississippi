namespace Mississippi.Core.Reducer;

/// <summary>
///     Convenience base class for reducers that understand exactly one event type.
/// </summary>
/// <typeparam name="TState">Aggregate-root state.</typeparam>
/// <typeparam name="TSupportedEvent">Event handled by the reducer.</typeparam>
public abstract class ReducerBase<TState, TSupportedEvent>
    : IReducer<TState>,
      IEventAwareReducer
{
    /// <inheritdoc />
    public Type SupportedEventType
    {
        get { return typeof(TSupportedEvent); }
    }

    /// <inheritdoc />
    public bool CanHandle(
        Type eventType
    )
    {
        return SupportedEventType.IsAssignableFrom(eventType);
    }

    /// <inheritdoc />
    public virtual TState Reduce<TInputEvent>(
        TState state,
        TInputEvent eventToReduce
    )
    {
        if (eventToReduce is TSupportedEvent typedEvent)
        {
            return ReduceInner(state, typedEvent);
        }

        return state;
    }

    /// <summary>
    ///     Reducer-specific domain logic.
    /// </summary>
    /// <param name="state">Current state.</param>
    /// <param name="eventToReduce">Event to apply.</param>
    /// <returns>The next state.</returns>
    protected abstract TState ReduceInner(
        TState state,
        TSupportedEvent eventToReduce
    );
}