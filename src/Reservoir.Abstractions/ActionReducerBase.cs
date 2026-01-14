using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Reservoir.Abstractions;

/// <summary>
///     Base class for action reducers that handle a specific action type.
/// </summary>
/// <typeparam name="TAction">The action type this action reducer handles.</typeparam>
/// <typeparam name="TState">The state type this action reducer produces.</typeparam>
/// <remarks>
///     <para>
///         Inherit from this class to implement an action reducer. Override <see cref="Reduce" />
///         to define how the action transforms the state.
///     </para>
///     <para>
///         The base class provides the <see cref="TryReduce" /> implementation that performs
///         type checking and delegates to your <see cref="Reduce" /> method.
///     </para>
/// </remarks>
public abstract class ActionReducerBase<TAction, TState> : IActionReducer<TAction, TState>
    where TAction : IAction
    where TState : class
{
    /// <inheritdoc />
    public abstract TState Reduce(
        TState state,
        TAction action
    );

    /// <inheritdoc />
    public bool TryReduce(
        TState state,
        IAction action,
        out TState newState
    )
    {
        if (action is TAction typedAction)
        {
            newState = Reduce(state, typedAction);
            return true;
        }

        newState = state;
        return false;
    }
}