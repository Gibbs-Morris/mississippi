using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Reservoir.Abstractions;

/// <summary>
///     Defines an action reducer that attempts to transform an incoming action into a new state.
/// </summary>
/// <typeparam name="TState">The state type produced by the action reducer.</typeparam>
/// <remarks>
///     <para>
///         This interface provides a type-erased entry point for the action reducer, allowing
///         the root action reducer to dispatch actions without knowing their concrete types.
///     </para>
/// </remarks>
public interface IActionReducer<TState>
    where TState : class
{
    /// <summary>
    ///     Attempts to apply an action to the current state to produce the next state.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The action to reduce.</param>
    /// <param name="newState">The new state produced when reduction succeeds.</param>
    /// <returns>
    ///     A value indicating whether the action reducer can handle the supplied action and produced a new state.
    /// </returns>
    bool TryReduce(
        TState state,
        IAction action,
        out TState newState
    );
}

/// <summary>
///     Defines an action reducer that transforms a specific action type into a new state.
/// </summary>
/// <typeparam name="TAction">The action type consumed by the action reducer.</typeparam>
/// <typeparam name="TState">The state type produced by the action reducer.</typeparam>
/// <remarks>
///     <para>
///         This is the primary interface for implementing action reducers.
///         Each action reducer handles exactly one action type, making them easy to test and reason about.
///     </para>
///     <para>
///         Action reducers must be pure functions: given the same state and action, they must always
///         return the same new state. They must not have side effects.
///     </para>
/// </remarks>
public interface IActionReducer<in TAction, TState> : IActionReducer<TState>
    where TAction : IAction
    where TState : class
{
    /// <summary>
    ///     Reduces the action into the current state, producing a new state.
    /// </summary>
    /// <param name="state">The current state before applying the action.</param>
    /// <param name="action">The action to apply.</param>
    /// <returns>The new state after applying the action.</returns>
    TState Reduce(
        TState state,
        TAction action
    );
}