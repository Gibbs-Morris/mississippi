using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir;

/// <summary>
///     Root-level action effect dispatcher that composes one or more
///     <see cref="IActionEffect{TState}" /> instances.
/// </summary>
/// <typeparam name="TState">The feature state type.</typeparam>
/// <remarks>
///     <para>
///         Effects are dispatched using a precomputed type index built at construction time.
///         For each action type, only effects registered for that exact type are considered.
///         All matching effects are invoked (unlike reducers which use first-match-wins).
///     </para>
///     <para>
///         This pattern mirrors <see cref="RootReducer{TState}" /> for consistency across the framework.
///     </para>
/// </remarks>
internal sealed class RootActionEffect<TState> : IRootActionEffect<TState>
    where TState : class, IFeatureState
{
    private static readonly Type StateType = typeof(TState);

    private readonly FrozenDictionary<Type, ImmutableArray<IActionEffect<TState>>> effectIndex;

    private readonly ImmutableArray<IActionEffect<TState>> fallbackEffects;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RootActionEffect{TState}" /> class.
    /// </summary>
    /// <param name="effects">The effects that can handle actions for this feature state.</param>
    public RootActionEffect(
        IEnumerable<IActionEffect<TState>> effects
    )
    {
        ArgumentNullException.ThrowIfNull(effects);
        IActionEffect<TState>[] effectsArray = effects.ToArray();
        (effectIndex, fallbackEffects) = BuildEffectIndex(effectsArray);
        HasEffects = effectsArray.Length > 0;
    }

    /// <inheritdoc />
    public bool HasEffects { get; }

    /// <summary>
    ///     Builds an index mapping action types to their effects, preserving registration order.
    /// </summary>
    private static (FrozenDictionary<Type, ImmutableArray<IActionEffect<TState>>> Index,
        ImmutableArray<IActionEffect<TState>> Fallback) BuildEffectIndex(
            IActionEffect<TState>[] effectsArray
        )
    {
        Dictionary<Type, ImmutableArray<IActionEffect<TState>>.Builder> indexBuilder = new();
        ImmutableArray<IActionEffect<TState>>.Builder fallbackBuilder =
            ImmutableArray.CreateBuilder<IActionEffect<TState>>();
        foreach (IActionEffect<TState> effect in effectsArray)
        {
            Type? actionType = ExtractActionType(effect.GetType());
            if (actionType is not null)
            {
                if (!indexBuilder.TryGetValue(actionType, out ImmutableArray<IActionEffect<TState>>.Builder? list))
                {
                    list = ImmutableArray.CreateBuilder<IActionEffect<TState>>();
                    indexBuilder[actionType] = list;
                }

                list.Add(effect);
            }
            else
            {
                fallbackBuilder.Add(effect);
            }
        }

        FrozenDictionary<Type, ImmutableArray<IActionEffect<TState>>> frozenIndex =
            indexBuilder.ToFrozenDictionary(kvp => kvp.Key, kvp => kvp.Value.ToImmutable());
        return (frozenIndex, fallbackBuilder.ToImmutable());
    }

    /// <summary>
    ///     Dispatches an action to a collection of effects.
    /// </summary>
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Effects are responsible for their own error handling; store must remain stable")]
    private static async IAsyncEnumerable<IAction> DispatchToEffectsAsync(
        ImmutableArray<IActionEffect<TState>> effects,
        IAction action,
        TState currentState,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        foreach (IActionEffect<TState> effect in effects)
        {
            if (!effect.CanHandle(action))
            {
                continue;
            }

            IAsyncEnumerator<IAction>? enumerator = null;
            try
            {
                enumerator = effect.HandleAsync(action, currentState, cancellationToken)
                    .GetAsyncEnumerator(cancellationToken);
                while (true)
                {
                    IAction? yieldedAction = null;
                    try
                    {
                        if (!await enumerator.MoveNextAsync())
                        {
                            break;
                        }

                        yieldedAction = enumerator.Current;
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected when effect is cancelled; don't propagate
                        break;
                    }
                    catch (Exception)
                    {
                        // Effect threw; break out of this effect's enumeration but continue with other effects
                        break;
                    }

                    if (yieldedAction is not null)
                    {
                        yield return yieldedAction;
                    }
                }
            }
            finally
            {
                if (enumerator is not null)
                {
                    await enumerator.DisposeAsync();
                }
            }
        }
    }

    /// <summary>
    ///     Extracts the TAction type argument from an effect implementing ActionEffectBase{TAction, TState}.
    /// </summary>
    private static Type? ExtractActionType(
        Type effectType
    )
    {
        // Walk up the inheritance chain looking for ActionEffectBase<TAction, TState>
        // or SimpleActionEffectBase<TAction, TState>
        Type? current = effectType.BaseType;
        while (current is not null)
        {
            if (current.IsGenericType)
            {
                Type genericDef = current.GetGenericTypeDefinition();
                bool isActionEffectBase = (genericDef.Name == "ActionEffectBase`2") &&
                                          (genericDef.Namespace == "Mississippi.Reservoir.Abstractions");
                bool isSimpleActionEffectBase = (genericDef.Name == "SimpleActionEffectBase`2") &&
                                                (genericDef.Namespace == "Mississippi.Reservoir.Abstractions");
                if (isActionEffectBase || isSimpleActionEffectBase)
                {
                    Type[] typeArgs = current.GetGenericArguments();

                    // typeArgs[0] = TAction, typeArgs[1] = TState
                    if ((typeArgs.Length == 2) && (typeArgs[1] == StateType))
                    {
                        return typeArgs[0];
                    }
                }
            }

            current = current.BaseType;
        }

        return null;
    }

    /// <inheritdoc />
    public IAsyncEnumerable<IAction> HandleAsync(
        IAction action,
        TState currentState,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(action);
        return HandleCoreAsync(action, currentState, cancellationToken);
    }

    private async IAsyncEnumerable<IAction> HandleCoreAsync(
        IAction action,
        TState currentState,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        Type actionRuntimeType = action.GetType();

        // Fast path: look up effects registered for this exact action type
        if (effectIndex.TryGetValue(actionRuntimeType, out ImmutableArray<IActionEffect<TState>> indexed))
        {
            await foreach (IAction yieldedAction in DispatchToEffectsAsync(
                               indexed,
                               action,
                               currentState,
                               cancellationToken))
            {
                yield return yieldedAction;
            }
        }

        // Slow path: iterate fallback effects whose action type could not be determined at construction
        await foreach (IAction yieldedAction in DispatchToEffectsAsync(
                           fallbackEffects,
                           action,
                           currentState,
                           cancellationToken))
        {
            yield return yieldedAction;
        }
    }
}