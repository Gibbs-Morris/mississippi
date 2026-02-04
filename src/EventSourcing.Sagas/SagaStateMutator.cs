using System;
using System.Collections.Concurrent;

using Mississippi.EventSourcing.Sagas.Abstractions;


namespace Mississippi.EventSourcing.Sagas;

/// <summary>
///     Applies saga infrastructure event data to saga state instances.
/// </summary>
internal static class SagaStateMutator
{
    private static readonly ConcurrentDictionary<Type, SagaStatePropertyMap> PropertyMaps = new();

    /// <summary>
    ///     Creates an updated saga state by copying existing values and applying an update action.
    /// </summary>
    /// <typeparam name="TSaga">The saga state type.</typeparam>
    /// <param name="state">The existing saga state, if any.</param>
    /// <param name="update">The update action to apply.</param>
    /// <returns>The updated saga state instance.</returns>
    public static TSaga CreateUpdated<TSaga>(
        TSaga? state,
        Action<SagaStatePropertyMap, TSaga> update
    )
        where TSaga : class, ISagaState
    {
        ArgumentNullException.ThrowIfNull(update);
        TSaga instance = CreateInstance<TSaga>();
        SagaStatePropertyMap map = GetMap(typeof(TSaga));
        if (state is not null)
        {
            map.CopyValues(state, instance);
        }

        update(map, instance);
        return instance;
    }

    private static TSaga CreateInstance<TSaga>()
        where TSaga : class, ISagaState
    {
        TSaga? instance = Activator.CreateInstance<TSaga>();
        if (instance is not null)
        {
            return instance;
        }

        throw new InvalidOperationException(
            $"Saga state type '{typeof(TSaga).FullName}' must have a parameterless constructor.");
    }

    private static SagaStatePropertyMap GetMap(
        Type stateType
    ) =>
        PropertyMaps.GetOrAdd(stateType, static type => new(type));
}