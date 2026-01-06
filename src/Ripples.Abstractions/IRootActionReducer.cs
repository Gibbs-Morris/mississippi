using Mississippi.Ripples.Abstractions.Actions;


namespace Mississippi.Ripples.Abstractions;

/// <summary>
///     Interface for root-level reducers that process actions and update feature state.
///     Routes actions to the appropriate typed reducer.
/// </summary>
/// <typeparam name="TState">The type of the feature state being reduced.</typeparam>
public interface IRootActionReducer<TState>
    where TState : class
{
    /// <summary>
    ///     Reduces an action into the current state, producing a new state.
    ///     This method is called for each action to update the feature state.
    /// </summary>
    /// <param name="state">The current state before applying the action.</param>
    /// <param name="action">The action to be processed and applied to the state.</param>
    /// <returns>The new state after applying the action, or the same state if no reducer matched.</returns>
    TState Reduce(
        TState state,
        IAction action
    );
}