using System;

using Mississippi.Ripples.Abstractions.Actions;


namespace Mississippi.Ripples.Abstractions;

/// <summary>
///     Provides a base class for reducers that transform an incoming action into a new state.
/// </summary>
/// <typeparam name="TAction">The action type consumed by the reducer.</typeparam>
/// <typeparam name="TState">The state type produced by the reducer.</typeparam>
/// <remarks>
///     <para>
///         This base class mirrors the server-side <c>Reducer&lt;TEvent, TProjection&gt;</c> pattern,
///         providing the same unified developer experience across client and server.
///     </para>
///     <para>
///         Includes a mutation guard that ensures reducers return new state instances rather than
///         mutating the existing state.
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
public abstract class ActionReducer<TAction, TState> : IActionReducer<TAction, TState>
    where TAction : IAction
    where TState : class
{
    /// <inheritdoc />
    public TState Reduce(
        TState state,
        TAction action
    )
    {
        ArgumentNullException.ThrowIfNull(action);
        return ReduceCore(state, action);
    }

    /// <inheritdoc />
    public bool TryReduce(
        TState state,
        IAction action,
        out TState newState
    )
    {
        ArgumentNullException.ThrowIfNull(action);
        if (action is not TAction typedAction)
        {
            newState = default!;
            return false;
        }

        newState = Reduce(state, typedAction);
        return true;
    }

    /// <summary>
    ///     Applies an action to the current state to produce the next state.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The action to reduce.</param>
    /// <returns>The new state produced from applying the action.</returns>
    protected abstract TState ReduceCore(
        TState state,
        TAction action
    );
}