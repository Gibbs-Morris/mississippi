using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

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
        EffectCount = effectsArray.Length;
        HasEffects = effectsArray.Length > 0;
    }

    /// <inheritdoc />
    public int EffectCount { get; }

    /// <inheritdoc />
    public bool HasEffects { get; }

    /// <summary>
    ///     Adds an effect to the appropriate index or fallback collection.
    /// </summary>
    private static void AddEffectToIndex(
        IActionEffect<TState> effect,
        Dictionary<Type, ImmutableArray<IActionEffect<TState>>.Builder> indexBuilder,
        ImmutableArray<IActionEffect<TState>>.Builder fallbackBuilder
    )
    {
        Type? actionType = ExtractActionType(effect.GetType());
        if (actionType is null)
        {
            fallbackBuilder.Add(effect);
            return;
        }

        if (!indexBuilder.TryGetValue(actionType, out ImmutableArray<IActionEffect<TState>>.Builder? list))
        {
            list = ImmutableArray.CreateBuilder<IActionEffect<TState>>();
            indexBuilder[actionType] = list;
        }

        list.Add(effect);
    }

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
            AddEffectToIndex(effect, indexBuilder, fallbackBuilder);
        }

        FrozenDictionary<Type, ImmutableArray<IActionEffect<TState>>> frozenIndex =
            indexBuilder.ToFrozenDictionary(kvp => kvp.Key, kvp => kvp.Value.ToImmutable());
        return (frozenIndex, fallbackBuilder.ToImmutable());
    }

    /// <summary>
    ///     Dispatches an action to a collection of effects.
    /// </summary>
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

            await foreach (IAction yieldedAction in EnumerateEffectSafelyAsync(
                               effect,
                               action,
                               currentState,
                               cancellationToken))
            {
                yield return yieldedAction;
            }
        }
    }

    /// <summary>
    ///     Safely enumerates an effect's results, catching exceptions per-effect.
    /// </summary>
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Effects are responsible for their own error handling; store must remain stable")]
    private static async IAsyncEnumerable<IAction> EnumerateEffectSafelyAsync(
        IActionEffect<TState> effect,
        IAction action,
        TState currentState,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        IAsyncEnumerator<IAction>? enumerator = null;
        try
        {
            enumerator = effect.HandleAsync(action, currentState, cancellationToken)
                .GetAsyncEnumerator(cancellationToken);
            while (await TryMoveNextAsync(enumerator))
            {
                yield return enumerator.Current;
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
            Type? actionType = TryExtractActionTypeFromBase(current);
            if (actionType is not null)
            {
                return actionType;
            }

            current = current.BaseType;
        }

        return null;
    }

    /// <summary>
    ///     Determines if the generic type definition is ActionEffectBase or SimpleActionEffectBase.
    /// </summary>
    /// <remarks>
    ///     Uses namespace and name checks because this is runtime reflection (not Roslyn analysis).
    ///     The approach is stable as long as the base class names don't change.
    /// </remarks>
    private static bool IsActionEffectBaseType(
        Type genericDef
    )
    {
        const string expectedNamespace = "Mississippi.Reservoir.Abstractions";
        return (genericDef.Namespace == expectedNamespace) &&
               genericDef.Name is "ActionEffectBase`2" or "SimpleActionEffectBase`2";
    }

    /// <summary>
    ///     Determines if an exception is critical and should not be swallowed.
    /// </summary>
    /// <remarks>
    ///     Critical exceptions indicate catastrophic failures that should propagate
    ///     rather than being silently swallowed. These include memory exhaustion,
    ///     stack overflow, and thread abort conditions.
    /// </remarks>
    private static bool IsCriticalException(
        Exception ex
    ) =>
        ex is OutOfMemoryException or StackOverflowException or ThreadInterruptedException;

    /// <summary>
    ///     Attempts to extract the TAction type from a base type if it matches ActionEffectBase or SimpleActionEffectBase.
    /// </summary>
    private static Type? TryExtractActionTypeFromBase(
        Type baseType
    )
    {
        if (!baseType.IsGenericType)
        {
            return null;
        }

        Type genericDef = baseType.GetGenericTypeDefinition();
        if (!IsActionEffectBaseType(genericDef))
        {
            return null;
        }

        Type[] typeArgs = baseType.GetGenericArguments();
        if ((typeArgs.Length == 2) && (typeArgs[1] == StateType))
        {
            return typeArgs[0];
        }

        return null;
    }

    /// <summary>
    ///     Attempts to move the enumerator to the next element, swallowing non-critical exceptions.
    /// </summary>
    private static async Task<bool> TryMoveNextAsync(
        IAsyncEnumerator<IAction> enumerator
    )
    {
        try
        {
            return await enumerator.MoveNextAsync();
        }
        catch (OperationCanceledException)
        {
            // Expected when effect is cancelled
            return false;
        }
        catch (Exception ex) when (!IsCriticalException(ex))
        {
            // Effect threw non-critical exception; stop enumerating this effect.
            // Critical exceptions (OOM, StackOverflow, etc.) will propagate.
            return false;
        }
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