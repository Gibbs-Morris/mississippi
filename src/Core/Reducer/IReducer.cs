namespace Mississippi.EventSourcing.Reducer;

/// <summary>
///     Defines a pure function that converts <typeparamref name="TState" /> + event → next state.
/// </summary>
/// <typeparam name="TState">Aggregate-root state type.</typeparam>
public interface IReducer<TState>
{
    /// <summary>
    ///     Applies <paramref name="eventToReduce" /> to <paramref name="state" />.
    /// </summary>
    /// <typeparam name="TEvent">Concrete event type.</typeparam>
    /// <param name="state">Current state instance.</param>
    /// <param name="eventToReduce">Event to be applied.</param>
    /// <returns>The next state.</returns>
    TState Reduce<TEvent>(
        TState state,
        TEvent eventToReduce
    );
}