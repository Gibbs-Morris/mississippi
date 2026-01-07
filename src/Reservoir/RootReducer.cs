using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Reservoir;

/// <summary>
///     Root-level reducer that composes one or more <see cref="IReducer{TState}" /> instances.
/// </summary>
/// <typeparam name="TState">The feature state type.</typeparam>
/// <remarks>
///     <para>
///         Actions are dispatched using a precomputed type index built at construction time.
///         For each action type, only reducers registered for that exact type are considered.
///     </para>
/// </remarks>
public sealed class RootReducer<TState> : IRootReducer<TState>
    where TState : class
{
    private static readonly Type StateType = typeof(TState);

    private readonly ImmutableArray<IReducer<TState>> fallbackReducers;

    private readonly FrozenDictionary<Type, ImmutableArray<IReducer<TState>>> reducerIndex;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RootReducer{TState}" /> class.
    /// </summary>
    /// <param name="reducers">The reducers that can update the feature state.</param>
    public RootReducer(
        IEnumerable<IReducer<TState>> reducers
    )
    {
        ArgumentNullException.ThrowIfNull(reducers);
        IReducer<TState>[] reducersArray = reducers.ToArray();
        (reducerIndex, fallbackReducers) = BuildReducerIndex(reducersArray);
    }

    /// <summary>
    ///     Builds an index mapping action types to their reducers, preserving registration order.
    /// </summary>
    private static (FrozenDictionary<Type, ImmutableArray<IReducer<TState>>> Index, ImmutableArray<IReducer<TState>>
        Fallback) BuildReducerIndex(
            IReducer<TState>[] reducersArray
        )
    {
        Dictionary<Type, ImmutableArray<IReducer<TState>>.Builder> indexBuilder = new();
        ImmutableArray<IReducer<TState>>.Builder fallbackBuilder = ImmutableArray.CreateBuilder<IReducer<TState>>();
        foreach (IReducer<TState> reducer in reducersArray)
        {
            Type? actionType = ExtractActionType(reducer.GetType());
            if (actionType is not null)
            {
                if (!indexBuilder.TryGetValue(actionType, out ImmutableArray<IReducer<TState>>.Builder? list))
                {
                    list = ImmutableArray.CreateBuilder<IReducer<TState>>();
                    indexBuilder[actionType] = list;
                }

                list.Add(reducer);
            }
            else
            {
                fallbackBuilder.Add(reducer);
            }
        }

        FrozenDictionary<Type, ImmutableArray<IReducer<TState>>> frozenIndex =
            indexBuilder.ToFrozenDictionary(kvp => kvp.Key, kvp => kvp.Value.ToImmutable());
        return (frozenIndex, fallbackBuilder.ToImmutable());
    }

    /// <summary>
    ///     Extracts the TAction type argument from a reducer implementing IReducer{TAction, TState}.
    /// </summary>
    private static Type? ExtractActionType(
        Type reducerType
    )
    {
        Type genericInterface = typeof(IReducer<,>);
        foreach (Type iface in reducerType.GetInterfaces())
        {
            if (!iface.IsGenericType)
            {
                continue;
            }

            if (iface.GetGenericTypeDefinition() != genericInterface)
            {
                continue;
            }

            Type[] args = iface.GetGenericArguments();

            // Second generic argument is TState, verify it matches
            if ((args.Length == 2) && args[1].IsAssignableFrom(StateType))
            {
                return args[0]; // TAction
            }
        }

        return null;
    }

    /// <inheritdoc />
    public TState Reduce(
        TState state,
        IAction action
    )
    {
        ArgumentNullException.ThrowIfNull(action);
        TState currentState = state;

        // First, try indexed reducers for the exact action type
        Type actionType = action.GetType();
        if (reducerIndex.TryGetValue(actionType, out ImmutableArray<IReducer<TState>> indexedReducers))
        {
            foreach (IReducer<TState> reducer in indexedReducers)
            {
                if (reducer.TryReduce(currentState, action, out TState newState))
                {
                    currentState = newState;
                }
            }
        }

        // Then, try fallback reducers (those without specific action type)
        foreach (IReducer<TState> reducer in fallbackReducers)
        {
            if (reducer.TryReduce(currentState, action, out TState newState))
            {
                currentState = newState;
            }
        }

        return currentState;
    }
}