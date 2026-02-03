using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Mississippi.EventSourcing.Reducers.Abstractions;
using Mississippi.EventSourcing.Sagas.Abstractions;

namespace Mississippi.EventSourcing.Sagas;

/// <summary>
///     Applies saga infrastructure events to update saga state.
/// </summary>
internal static class SagaStateMutator
{
    private static readonly ConcurrentDictionary<Type, SagaStatePropertyMap> PropertyMaps = new();

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
        object? instance = Activator.CreateInstance(typeof(TSaga));
        if (instance is TSaga saga)
        {
            return saga;
        }

        throw new InvalidOperationException(
            $"Saga state type '{typeof(TSaga).FullName}' must have a parameterless constructor.");
    }

    private static SagaStatePropertyMap GetMap(
        Type stateType
    ) => PropertyMaps.GetOrAdd(stateType, static type => new SagaStatePropertyMap(type));
}

internal sealed class SagaStatePropertyMap
{
    private readonly PropertyInfo[] settableProperties;

    private readonly IReadOnlyDictionary<string, PropertyInfo> propertyLookup;

    public SagaStatePropertyMap(
        Type stateType
    )
    {
        ArgumentNullException.ThrowIfNull(stateType);
        settableProperties = stateType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.CanRead && property.CanWrite)
            .ToArray();
        propertyLookup = settableProperties.ToDictionary(property => property.Name, StringComparer.Ordinal);
    }

    public void CopyValues(
        object source,
        object target
    )
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        foreach (PropertyInfo property in settableProperties)
        {
            object? value = property.GetValue(source);
            property.SetValue(target, value);
        }
    }

    public void SetProperty(
        object target,
        string propertyName,
        object? value
    )
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        if (!propertyLookup.TryGetValue(propertyName, out PropertyInfo? property))
        {
            throw new InvalidOperationException($"Property '{propertyName}' not found on saga state type.");
        }

        property.SetValue(target, value);
    }
}

/// <summary>
///     Reducer for <see cref="SagaStartedEvent" />.
/// </summary>
public sealed class SagaStartedReducer<TSaga> : EventReducerBase<SagaStartedEvent, TSaga>
    where TSaga : class, ISagaState
{
    /// <inheritdoc />
    protected override TSaga ReduceCore(
        TSaga state,
        SagaStartedEvent eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        return SagaStateMutator.CreateUpdated(state, (map, instance) =>
        {
            map.SetProperty(instance, nameof(ISagaState.SagaId), eventData.SagaId);
            map.SetProperty(instance, nameof(ISagaState.Phase), SagaPhase.Running);
            map.SetProperty(instance, nameof(ISagaState.LastCompletedStepIndex), -1);
            map.SetProperty(instance, nameof(ISagaState.CorrelationId), eventData.CorrelationId);
            map.SetProperty(instance, nameof(ISagaState.StartedAt), eventData.StartedAt);
            map.SetProperty(instance, nameof(ISagaState.StepHash), eventData.StepHash);
        });
    }
}

/// <summary>
///     Reducer for <see cref="SagaStepCompleted" />.
/// </summary>
public sealed class SagaStepCompletedReducer<TSaga> : EventReducerBase<SagaStepCompleted, TSaga>
    where TSaga : class, ISagaState
{
    /// <inheritdoc />
    protected override TSaga ReduceCore(
        TSaga state,
        SagaStepCompleted eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        return SagaStateMutator.CreateUpdated(state, (map, instance) =>
            map.SetProperty(instance, nameof(ISagaState.LastCompletedStepIndex), eventData.StepIndex));
    }
}

/// <summary>
///     Reducer for <see cref="SagaCompensating" />.
/// </summary>
public sealed class SagaCompensatingReducer<TSaga> : EventReducerBase<SagaCompensating, TSaga>
    where TSaga : class, ISagaState
{
    /// <inheritdoc />
    protected override TSaga ReduceCore(
        TSaga state,
        SagaCompensating eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        return SagaStateMutator.CreateUpdated(state, (map, instance) =>
            map.SetProperty(instance, nameof(ISagaState.Phase), SagaPhase.Compensating));
    }
}

/// <summary>
///     Reducer for <see cref="SagaCompleted" />.
/// </summary>
public sealed class SagaCompletedReducer<TSaga> : EventReducerBase<SagaCompleted, TSaga>
    where TSaga : class, ISagaState
{
    /// <inheritdoc />
    protected override TSaga ReduceCore(
        TSaga state,
        SagaCompleted eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        return SagaStateMutator.CreateUpdated(state, (map, instance) =>
            map.SetProperty(instance, nameof(ISagaState.Phase), SagaPhase.Completed));
    }
}

/// <summary>
///     Reducer for <see cref="SagaCompensated" />.
/// </summary>
public sealed class SagaCompensatedReducer<TSaga> : EventReducerBase<SagaCompensated, TSaga>
    where TSaga : class, ISagaState
{
    /// <inheritdoc />
    protected override TSaga ReduceCore(
        TSaga state,
        SagaCompensated eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        return SagaStateMutator.CreateUpdated(state, (map, instance) =>
            map.SetProperty(instance, nameof(ISagaState.Phase), SagaPhase.Compensated));
    }
}

/// <summary>
///     Reducer for <see cref="SagaFailed" />.
/// </summary>
public sealed class SagaFailedReducer<TSaga> : EventReducerBase<SagaFailed, TSaga>
    where TSaga : class, ISagaState
{
    /// <inheritdoc />
    protected override TSaga ReduceCore(
        TSaga state,
        SagaFailed eventData
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        return SagaStateMutator.CreateUpdated(state, (map, instance) =>
            map.SetProperty(instance, nameof(ISagaState.Phase), SagaPhase.Failed));
    }
}
