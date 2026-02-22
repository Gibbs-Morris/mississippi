using System;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Reservoir;

/// <summary>
///     An action reducer implemented as a delegate.
/// </summary>
/// <typeparam name="TAction">The action type this action reducer handles.</typeparam>
/// <typeparam name="TState">The state type this action reducer produces.</typeparam>
public sealed class DelegateActionReducer<TAction, TState> : IActionReducer<TAction, TState>
    where TAction : IAction
    where TState : class
{
    private readonly Func<TState, TAction, TState> reduce;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DelegateActionReducer{TAction,TState}" /> class.
    /// </summary>
    /// <param name="reduce">The reduce function.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="reduce" /> is null.</exception>
    public DelegateActionReducer(
        Func<TState, TAction, TState> reduce
    )
    {
        ArgumentNullException.ThrowIfNull(reduce);
        this.reduce = reduce;
    }

    /// <inheritdoc />
    public TState Reduce(
        TState state,
        TAction action
    ) =>
        reduce(state, action);

    /// <inheritdoc />
    public bool TryReduce(
        TState state,
        IAction action,
        out TState newState
    )
    {
        if (action is TAction typedAction)
        {
            newState = reduce(state, typedAction);
            return true;
        }

        newState = state;
        return false;
    }
}