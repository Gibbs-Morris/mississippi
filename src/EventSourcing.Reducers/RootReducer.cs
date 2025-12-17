using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Mississippi.EventSourcing.Reducers;

/// <summary>
///     Root-level reducer that composes one or more <see cref="IReducer{TProjection}" /> instances.
/// </summary>
/// <typeparam name="TProjection">The projection state type.</typeparam>
/// <remarks>
///     <para>
///         Events are dispatched using a precomputed type index built at construction time.
///         For each event type, only reducers registered for that exact type are considered,
///         preserving first-match-wins semantics within the original registration order.
///     </para>
/// </remarks>
public sealed class RootReducer<TProjection> : IRootReducer<TProjection>
{
    private static readonly Type ProjectionType = typeof(TProjection);

    private readonly ImmutableArray<IReducer<TProjection>> fallbackReducers;

    private readonly string reducerHash;

    private readonly FrozenDictionary<Type, ImmutableArray<IReducer<TProjection>>> reducerIndex;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RootReducer{TProjection}" /> class.
    /// </summary>
    /// <param name="reducers">The reducers that can update the projection state.</param>
    /// <param name="logger">The logger used for reducer diagnostics.</param>
    public RootReducer(
        IEnumerable<IReducer<TProjection>> reducers,
        ILogger<RootReducer<TProjection>>? logger = null
    )
    {
        ArgumentNullException.ThrowIfNull(reducers);
        IReducer<TProjection>[] reducersArray = reducers.ToArray();
        Logger = logger ?? NullLogger<RootReducer<TProjection>>.Instance;
        reducerHash = ComputeReducerHash(reducersArray);
        (reducerIndex, fallbackReducers) = BuildReducerIndex(reducersArray);
    }

    private ILogger<RootReducer<TProjection>> Logger { get; }

    /// <summary>
    ///     Builds an index mapping event types to their reducers, preserving registration order.
    /// </summary>
    private static (FrozenDictionary<Type, ImmutableArray<IReducer<TProjection>>> Index,
        ImmutableArray<IReducer<TProjection>> Fallback) BuildReducerIndex(
            IReducer<TProjection>[] reducersArray
        )
    {
        Dictionary<Type, ImmutableArray<IReducer<TProjection>>.Builder> indexBuilder = new();
        ImmutableArray<IReducer<TProjection>>.Builder fallbackBuilder =
            ImmutableArray.CreateBuilder<IReducer<TProjection>>();
        foreach (IReducer<TProjection> reducer in reducersArray)
        {
            Type? eventType = ExtractEventType(reducer.GetType());
            if (eventType is not null)
            {
                if (!indexBuilder.TryGetValue(eventType, out ImmutableArray<IReducer<TProjection>>.Builder? list))
                {
                    list = ImmutableArray.CreateBuilder<IReducer<TProjection>>();
                    indexBuilder[eventType] = list;
                }

                list.Add(reducer);
            }
            else
            {
                fallbackBuilder.Add(reducer);
            }
        }

        FrozenDictionary<Type, ImmutableArray<IReducer<TProjection>>> frozenIndex =
            indexBuilder.ToFrozenDictionary(kvp => kvp.Key, kvp => kvp.Value.ToImmutable());
        return (frozenIndex, fallbackBuilder.ToImmutable());
    }

    private static string ComputeReducerHash(
        IReadOnlyList<IReducer<TProjection>> reducers
    )
    {
        string[] typeNames = reducers.Select(x => x.GetType().FullName ?? x.GetType().Name)
            .Order(StringComparer.Ordinal)
            .ToArray();
        string input = string.Join("|", typeNames);
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }

    /// <summary>
    ///     Extracts the TEvent type argument from a reducer implementing IReducer{TEvent, TProjection}.
    /// </summary>
    private static Type? ExtractEventType(
        Type reducerType
    )
    {
        // Look for IReducer<TEvent, TProjection> in the interface list.
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

            Type[] typeArgs = iface.GetGenericArguments();

            // typeArgs[0] = TEvent, typeArgs[1] = TProjection
            if ((typeArgs.Length == 2) && (typeArgs[1] == ProjectionType))
            {
                return typeArgs[0];
            }
        }

        return null;
    }

    /// <inheritdoc />
    public string GetReducerHash() => reducerHash;

    /// <inheritdoc />
    public TProjection Reduce(
        TProjection state,
        object eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        string projectionType = ProjectionType.Name;
        Type eventRuntimeType = eventData.GetType();
        string eventType = eventRuntimeType.Name;
        Logger.RootReducerReducing(projectionType, eventType);

        // Fast path: look up reducers registered for this exact event type.
        if (reducerIndex.TryGetValue(eventRuntimeType, out ImmutableArray<IReducer<TProjection>> indexed) &&
            TryApplyReducers(indexed, state, eventData, projectionType, eventType, out TProjection result))
        {
            return result;
        }

        // Slow path: iterate fallback reducers whose event type could not be determined at construction.
        if (TryApplyReducers(fallbackReducers, state, eventData, projectionType, eventType, out TProjection fallbackResult))
        {
            return fallbackResult;
        }

        Logger.RootReducerNoReducerMatched(projectionType, eventType);
        return state;
    }

    /// <summary>
    ///     Attempts to apply the first matching reducer from the given collection.
    /// </summary>
    /// <param name="reducers">The collection of reducers to try.</param>
    /// <param name="state">The current projection state.</param>
    /// <param name="eventData">The event data to reduce.</param>
    /// <param name="projectionType">The projection type name for logging.</param>
    /// <param name="eventType">The event type name for logging.</param>
    /// <param name="result">The resulting projection if a reducer matched.</param>
    /// <returns>True if a reducer successfully handled the event; otherwise, false.</returns>
    private bool TryApplyReducers(
        IReadOnlyList<IReducer<TProjection>> reducers,
        TProjection state,
        object eventData,
        string projectionType,
        string eventType,
        out TProjection result
    )
    {
        foreach (IReducer<TProjection> reducer in reducers)
        {
            if (!reducer.TryReduce(state, eventData, out TProjection projection))
            {
                continue;
            }

            ValidateProjectionNotReused(state, projection, reducer.GetType().Name, projectionType, eventType);
            Logger.RootReducerReducerMatched(reducer.GetType().Name, eventType);
            result = projection;
            return true;
        }

        result = default!;
        return false;
    }

    /// <summary>
    ///     Validates that the projection instance was not reused (mutated in place).
    /// </summary>
    private void ValidateProjectionNotReused(
        TProjection state,
        TProjection projection,
        string reducerName,
        string projectionType,
        string eventType
    )
    {
        if (ProjectionType.IsValueType || state is null || !ReferenceEquals(state, projection))
        {
            return;
        }

        Logger.RootReducerProjectionInstanceReused(reducerName, projectionType, eventType);
        throw new InvalidOperationException(
            "Reducers must return a new projection instance. Use a copy/with expression instead of mutating state.");
    }
}