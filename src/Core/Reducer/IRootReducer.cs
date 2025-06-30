namespace Mississippi.EventSourcing.Reducer;

/// <summary>
///     Dispatches events to the correct reducers and folds the results.
/// </summary>
/// <typeparam name="TState">Aggregate-root state type.</typeparam>
public interface IRootReducer<TState>
{
    /// <summary>
    ///     Applies <paramref name="eventToReduce" /> to <paramref name="state" /> and
    ///     returns the resulting state.
    /// </summary>
    /// <typeparam name="TEvent">Concrete event type.</typeparam>
    /// <param name="state">Current state.</param>
    /// <param name="eventToReduce">Event to process.</param>
    /// <returns>The next state.</returns>
    TState Reduce<TEvent>(
        TState state,
        TEvent eventToReduce
    );
}