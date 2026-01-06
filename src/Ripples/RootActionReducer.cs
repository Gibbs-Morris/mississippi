using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Mississippi.Ripples.Abstractions;
using Mississippi.Ripples.Abstractions.Actions;


namespace Mississippi.Ripples;

/// <summary>
///     Root-level reducer that composes one or more <see cref="IActionReducer{TState}" /> instances.
/// </summary>
/// <typeparam name="TState">The feature state type.</typeparam>
/// <remarks>
///     <para>
///         Actions are dispatched using a precomputed type index built at construction time.
///         For each action type, only reducers registered for that exact type are considered.
///     </para>
///     <para>
///         This mirrors the server-side <c>RootReducer&lt;TProjection&gt;</c> pattern for
///         unified developer experience.
///     </para>
/// </remarks>
public sealed class RootActionReducer<TState> : IRootActionReducer<TState>
    where TState : class
{
    private static readonly Type StateType = typeof(TState);

    private readonly ImmutableArray<IActionReducer<TState>> fallbackReducers;

    private readonly FrozenDictionary<Type, ImmutableArray<IActionReducer<TState>>> reducerIndex;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RootActionReducer{TState}" /> class.
    /// </summary>
    /// <param name="reducers">The reducers that can update the feature state.</param>
    public RootActionReducer(
        IEnumerable<IActionReducer<TState>> reducers
    )
    {
        ArgumentNullException.ThrowIfNull(reducers);
        IActionReducer<TState>[] reducersArray = reducers.ToArray();
        (reducerIndex, fallbackReducers) = BuildReducerIndex(reducersArray);
    }

    /// <summary>
    ///     Builds an index mapping action types to their reducers, preserving registration order.
    /// </summary>
    private static (FrozenDictionary<Type, ImmutableArray<IActionReducer<TState>>> Index,
        ImmutableArray<IActionReducer<TState>> Fallback) BuildReducerIndex(
            IActionReducer<TState>[] reducersArray
        )
    {
        Dictionary<Type, ImmutableArray<IActionReducer<TState>>.Builder> indexBuilder = new();
        ImmutableArray<IActionReducer<TState>>.Builder fallbackBuilder =
            ImmutableArray.CreateBuilder<IActionReducer<TState>>();
        foreach (IActionReducer<TState> reducer in reducersArray)
        {
            Type? actionType = ExtractActionType(reducer.GetType());
            if (actionType is not null)
            {
                if (!indexBuilder.TryGetValue(actionType, out ImmutableArray<IActionReducer<TState>>.Builder? list))
                {
                    list = ImmutableArray.CreateBuilder<IActionReducer<TState>>();
                    indexBuilder[actionType] = list;
                }

                list.Add(reducer);
            }
            else
            {
                fallbackBuilder.Add(reducer);
            }
        }

        FrozenDictionary<Type, ImmutableArray<IActionReducer<TState>>> frozenIndex =
            indexBuilder.ToFrozenDictionary(kvp => kvp.Key, kvp => kvp.Value.ToImmutable());
        return (frozenIndex, fallbackBuilder.ToImmutable());
    }

    /// <summary>
    ///     Extracts the TAction type argument from a reducer implementing IActionReducer{TAction, TState}.
    /// </summary>
    private static Type? ExtractActionType(
        Type reducerType
    )
    {
        Type genericInterface = typeof(IActionReducer<,>);
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

            Type[] typeArgs = iface.GetGenericArguments();

            // typeArgs[0] = TAction, typeArgs[1] = TState
            if ((typeArgs.Length == 2) && (typeArgs[1] == StateType))
            {
                return typeArgs[0];
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
        Type actionRuntimeType = action.GetType();
        TState currentState = state;

        // First, try indexed reducers for this exact action type
        if (reducerIndex.TryGetValue(actionRuntimeType, out ImmutableArray<IActionReducer<TState>> indexedReducers))
        {
            foreach (IActionReducer<TState> reducer in indexedReducers)
            {
                if (reducer.TryReduce(currentState, action, out TState newState))
                {
                    currentState = newState;
                }
            }
        }

        // Then, try fallback reducers
        foreach (IActionReducer<TState> reducer in fallbackReducers)
        {
            if (reducer.TryReduce(currentState, action, out TState newState))
            {
                currentState = newState;
            }
        }

        return currentState;
    }
}