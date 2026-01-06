using Mississippi.Ripples.Abstractions.Actions;


namespace Mississippi.Ripples.Abstractions;

/// <summary>
///     Defines a reducer that attempts to transform an incoming action into a new state.
/// </summary>
/// <typeparam name="TState">The state type produced by the reducer.</typeparam>
/// <remarks>
///     <para>
///         This interface provides a type-erased entry point for the reducer, allowing
///         the root reducer to dispatch actions without knowing their concrete types.
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
    ///     A value indicating whether the reducer can handle the supplied action and produced a new state.
    /// </returns>
    bool TryReduce(
        TState state,
        IAction action,
        out TState newState
    );
}

/// <summary>
///     Defines a reducer that transforms an incoming action into a new state.
/// </summary>
/// <typeparam name="TAction">The action type consumed by the reducer.</typeparam>
/// <typeparam name="TState">The state type produced by the reducer.</typeparam>
/// <remarks>
///     <para>
///         This is the primary interface for implementing reducers with the unified pattern.
///         Each reducer handles exactly one action type, making them easy to test and reason about.
///     </para>
///     <para>
///         Reducers must be pure functions: given the same state and action, they must always
///         return the same new state. They must not have side effects.
///     </para>
/// </remarks>
/// <example>
///     <code>
///         public sealed class ToggleSidebarReducer : ActionReducer&lt;ToggleSidebarAction, SidebarState&gt;
///         {
///             protected override SidebarState ReduceCore(SidebarState state, ToggleSidebarAction action)
///                 =&gt; state with { IsOpen = !state.IsOpen };
///         }
///     </code>
/// </example>
public interface IActionReducer<in TAction, TState> : IActionReducer<TState>
    where TAction : IAction
    where TState : class
{
    /// <summary>
    ///     Applies an action to the current state to produce the next state.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The action to reduce.</param>
    /// <returns>The new state produced from applying the action.</returns>
    TState Reduce(
        TState state,
        TAction action
    );
}