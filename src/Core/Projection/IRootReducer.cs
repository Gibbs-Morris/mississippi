namespace Mississippi.Core.Projection;

/// <summary>
///     Interface for root-level projection reducers that process events and update projection state.
///     Provides methods for reducing events into projection state and generating reducer hashes for versioning.
/// </summary>
/// <typeparam name="T">The type of the projection state being reduced.</typeparam>
public interface IRootReducer<T>
{
    /// <summary>
    ///     Reduces an event into the current projection state, producing a new state.
    ///     This method is called for each event to update the projection.
    /// </summary>
    /// <param name="state">The current state of the projection before applying the event.</param>
    /// <param name="eventData">The event data to be processed and applied to the state.</param>
    /// <returns>The new projection state after applying the event.</returns>
    T Reduce(
        T state,
        object eventData
    );

    /// <summary>
    ///     Gets a hash value representing the current version of the reducer logic.
    ///     This hash is used to determine if the projection needs to be rebuilt when reducer logic changes.
    /// </summary>
    /// <returns>A string hash representing the reducer version.</returns>
    string GetReducerHash();
}