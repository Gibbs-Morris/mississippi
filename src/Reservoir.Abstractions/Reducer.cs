using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Reservoir.Abstractions;

/// <summary>
///     Base class for reducers that handle a specific action type.
/// </summary>
/// <typeparam name="TAction">The action type this reducer handles.</typeparam>
/// <typeparam name="TState">The state type this reducer produces.</typeparam>
/// <remarks>
///     <para>
///         Inherit from this class to implement a reducer. Override <see cref="Reduce" />
///         to define how the action transforms the state.
///     </para>
///     <para>
///         The base class provides the <see cref="TryReduce" /> implementation that performs
///         type checking and delegates to your <see cref="Reduce" /> method.
///     </para>
/// </remarks>
public abstract class Reducer<TAction, TState> : IReducer<TAction, TState>
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