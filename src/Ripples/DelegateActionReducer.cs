using System;

using Mississippi.Ripples.Abstractions;
using Mississippi.Ripples.Abstractions.Actions;


namespace Mississippi.Ripples;

/// <summary>
///     A reducer implementation that wraps a delegate function.
/// </summary>
/// <typeparam name="TAction">The action type consumed by the reducer.</typeparam>
/// <typeparam name="TState">The state type produced by the reducer.</typeparam>
internal sealed class DelegateActionReducer<TAction, TState> : IActionReducer<TAction, TState>
    where TAction : IAction
    where TState : class
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DelegateActionReducer{TAction, TState}" /> class.
    /// </summary>
    /// <param name="reduce">The delegate that performs the reduction.</param>
    public DelegateActionReducer(
        Func<TState, TAction, TState> reduce
    )
    {
        ArgumentNullException.ThrowIfNull(reduce);
        ReduceFunc = reduce;
    }

    private Func<TState, TAction, TState> ReduceFunc { get; }

    /// <inheritdoc />
    public TState Reduce(
        TState state,
        TAction action
    )
    {
        ArgumentNullException.ThrowIfNull(action);
        TState newState = ReduceFunc(state, action);

        // Mutation guard: ensure reducers return new instances
        if (state is not null && ReferenceEquals(state, newState))
        {
            throw new InvalidOperationException(
                "Reducers must return a new state instance. Use a copy/with expression instead of mutating state.");
        }

        return newState;
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
}