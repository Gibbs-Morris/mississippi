using System;
using System.Collections.Concurrent;

using Mississippi.EventSourcing.Aggregates.Abstractions;


namespace Mississippi.EventSourcing.Aggregates;

/// <summary>
///     Default implementation of <see cref="Abstractions.IEventTypeRegistry" /> using a concurrent dictionary.
/// </summary>
internal sealed class EventTypeRegistry : IEventTypeRegistry
{
    private readonly ConcurrentDictionary<string, Type> eventTypes = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public void Register(
        string eventName,
        Type eventType
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        ArgumentNullException.ThrowIfNull(eventType);
        eventTypes.TryAdd(eventName, eventType);
    }

    /// <inheritdoc />
    public Type? ResolveType(
        string eventTypeName
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventTypeName);
        return eventTypes.TryGetValue(eventTypeName, out Type? eventType) ? eventType : null;
    }
}