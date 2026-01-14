using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Reservoir.Abstractions;

/// <summary>
///     Interface for root-level action reducers that process actions and update feature state.
///     Routes actions to the appropriate typed action reducer.
/// </summary>
/// <typeparam name="TState">The type of the feature state being reduced.</typeparam>
public interface IRootReducer<TState>
    where TState : class
{
    /// <summary>
    ///     Reduces an action into the current state, producing a new state.
    ///     This method is called for each action to update the feature state.
    /// </summary>
    /// <param name="state">The current state before applying the action.</param>
    /// <param name="action">The action to be processed and applied to the state.</param>
    /// <returns>The new state after applying the action, or the same state if no action reducer matched.</returns>
    TState Reduce(
        TState state,
        IAction action
    );
}