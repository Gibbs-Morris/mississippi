using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Aggregates.Diagnostics;


namespace Mississippi.EventSourcing.Aggregates;

/// <summary>
///     Root-level event effect dispatcher that composes one or more <see cref="IEventEffect{TAggregate}" /> instances.
/// </summary>
/// <typeparam name="TAggregate">The aggregate state type.</typeparam>
/// <remarks>
///     <para>
///         Effects are dispatched using a precomputed type index built at construction time.
///         For each event type, only effects registered for that exact type are considered.
///         All matching effects are invoked (unlike reducers which use first-match-wins).
///     </para>
///     <para>
///         This pattern mirrors <see cref="Mississippi.EventSourcing.Reducers.RootReducer{TProjection}" />
///         for consistency across the framework.
///     </para>
/// </remarks>
public sealed class RootEventEffect<TAggregate> : IRootEventEffect<TAggregate>
{
    private static readonly Type AggregateType = typeof(TAggregate);

    private readonly FrozenDictionary<Type, ImmutableArray<IEventEffect<TAggregate>>> effectIndex;

    private readonly ImmutableArray<IEventEffect<TAggregate>> fallbackEffects;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RootEventEffect{TAggregate}" /> class.
    /// </summary>
    /// <param name="effects">The effects that can handle events for this aggregate.</param>
    /// <param name="logger">The logger used for effect dispatcher diagnostics.</param>
    public RootEventEffect(
        IEnumerable<IEventEffect<TAggregate>> effects,
        ILogger<RootEventEffect<TAggregate>>? logger = null
    )
    {
        ArgumentNullException.ThrowIfNull(effects);
        IEventEffect<TAggregate>[] effectsArray = effects.ToArray();
        Logger = logger ?? NullLogger<RootEventEffect<TAggregate>>.Instance;
        (effectIndex, fallbackEffects) = BuildEffectIndex(effectsArray);
        EffectCount = effectsArray.Length;
    }

    /// <inheritdoc />
    public int EffectCount { get; }

    private ILogger<RootEventEffect<TAggregate>> Logger { get; }

    /// <summary>
    ///     Builds an index mapping event types to their effects, preserving registration order.
    /// </summary>
    private static (FrozenDictionary<Type, ImmutableArray<IEventEffect<TAggregate>>> Index,
        ImmutableArray<IEventEffect<TAggregate>> Fallback) BuildEffectIndex(
            IEventEffect<TAggregate>[] effectsArray
        )
    {
        Dictionary<Type, ImmutableArray<IEventEffect<TAggregate>>.Builder> indexBuilder = new();
        ImmutableArray<IEventEffect<TAggregate>>.Builder fallbackBuilder =
            ImmutableArray.CreateBuilder<IEventEffect<TAggregate>>();
        foreach (IEventEffect<TAggregate> effect in effectsArray)
        {
            Type? eventType = ExtractEventType(effect.GetType());
            if (eventType is not null)
            {
                if (!indexBuilder.TryGetValue(eventType, out ImmutableArray<IEventEffect<TAggregate>>.Builder? list))
                {
                    list = ImmutableArray.CreateBuilder<IEventEffect<TAggregate>>();
                    indexBuilder[eventType] = list;
                }

                list.Add(effect);
            }
            else
            {
                fallbackBuilder.Add(effect);
            }
        }

        FrozenDictionary<Type, ImmutableArray<IEventEffect<TAggregate>>> frozenIndex =
            indexBuilder.ToFrozenDictionary(kvp => kvp.Key, kvp => kvp.Value.ToImmutable());
        return (frozenIndex, fallbackBuilder.ToImmutable());
    }

    /// <summary>
    ///     Dispatches an event to a collection of effects.
    /// </summary>
    private async IAsyncEnumerable<object> DispatchToEffectsAsync(
        ImmutableArray<IEventEffect<TAggregate>> effects,
        object eventData,
        TAggregate currentState,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        string eventTypeName = eventData.GetType().Name;
        string aggregateTypeName = AggregateType.Name;
        foreach (IEventEffect<TAggregate> effect in effects)
        {
            if (!effect.CanHandle(eventData))
            {
                continue;
            }

            await foreach (object yieldedEvent in EnumerateEffectSafelyAsync(
                               effect,
                               eventData,
                               currentState,
                               eventTypeName,
                               aggregateTypeName,
                               cancellationToken))
            {
                yield return yieldedEvent;
            }
        }
    }

    /// <summary>
    ///     Safely enumerates an effect's results, catching exceptions per-effect.
    /// </summary>
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Effects are responsible for their own error handling; grain must remain stable")]
    private async IAsyncEnumerable<object> EnumerateEffectSafelyAsync(
        IEventEffect<TAggregate> effect,
        object eventData,
        TAggregate currentState,
        string eventTypeName,
        string aggregateTypeName,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        string effectTypeName = effect.GetType().Name;
        IAsyncEnumerator<object>? enumerator = null;
        try
        {
            enumerator = effect.HandleAsync(eventData, currentState, cancellationToken)
                .GetAsyncEnumerator(cancellationToken);
            while (await TryMoveNextAsync(enumerator, effectTypeName, eventTypeName, aggregateTypeName))
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
    ///     Extracts the TEvent type argument from an effect implementing EventEffectBase{TEvent, TAggregate}.
    /// </summary>
    private static Type? ExtractEventType(
        Type effectType
    )
    {
        // Walk up the inheritance chain looking for EventEffectBase<TEvent, TAggregate>
        Type? current = effectType.BaseType;
        while (current is not null)
        {
            if (current.IsGenericType)
            {
                Type genericDef = current.GetGenericTypeDefinition();
                if ((genericDef.Name == "EventEffectBase`2") &&
                    (genericDef.Namespace == "Mississippi.EventSourcing.Aggregates.Abstractions"))
                {
                    Type[] typeArgs = current.GetGenericArguments();

                    // typeArgs[0] = TEvent, typeArgs[1] = TAggregate
                    if ((typeArgs.Length == 2) && (typeArgs[1] == AggregateType))
                    {
                        return typeArgs[0];
                    }
                }
            }

            current = current.BaseType;
        }

        return null;
    }

    /// <summary>
    ///     Attempts to move the enumerator to the next element, swallowing exceptions.
    /// </summary>
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Effects are responsible for their own error handling; grain must remain stable")]
    private async Task<bool> TryMoveNextAsync(
        IAsyncEnumerator<object> enumerator,
        string effectTypeName,
        string eventTypeName,
        string aggregateTypeName
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
        catch (Exception ex)
        {
            // Effect threw; log, record metric, and stop enumerating this effect
            Logger.EventEffectFailed(effectTypeName, eventTypeName, aggregateTypeName, ex);
            EventEffectMetrics.RecordEffectError(aggregateTypeName, effectTypeName, eventTypeName);
            return false;
        }
    }

    /// <inheritdoc />
    public IAsyncEnumerable<object> DispatchAsync(
        object eventData,
        TAggregate currentState,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        return DispatchCoreAsync(eventData, currentState, cancellationToken);
    }

    private async IAsyncEnumerable<object> DispatchCoreAsync(
        object eventData,
        TAggregate currentState,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        Type eventRuntimeType = eventData.GetType();
        string eventTypeName = eventRuntimeType.Name;
        string aggregateTypeName = AggregateType.Name;
        Logger.RootEventEffectDispatching(aggregateTypeName, eventTypeName);

        // Fast path: look up effects registered for this exact event type
        if (effectIndex.TryGetValue(eventRuntimeType, out ImmutableArray<IEventEffect<TAggregate>> indexed))
        {
            await foreach (object yieldedEvent in DispatchToEffectsAsync(
                               indexed,
                               eventData,
                               currentState,
                               cancellationToken))
            {
                yield return yieldedEvent;
            }
        }

        // Slow path: iterate fallback effects whose event type could not be determined at construction
        await foreach (object yieldedEvent in DispatchToEffectsAsync(
                           fallbackEffects,
                           eventData,
                           currentState,
                           cancellationToken))
        {
            yield return yieldedEvent;
        }
    }
}