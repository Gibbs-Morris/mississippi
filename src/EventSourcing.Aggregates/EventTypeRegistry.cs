using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;


namespace Mississippi.EventSourcing.Aggregates;

/// <summary>
///     Default implementation of <see cref="IEventTypeRegistry" /> providing bidirectional
///     event name ↔ CLR type resolution with O(1) lookup performance.
/// </summary>
/// <remarks>
///     <para>
///         This registry is populated at application startup through explicit registration
///         via <see cref="Register" /> or assembly scanning via <see cref="ScanAssembly" />.
///         Once populated, lookups are lock-free dictionary reads.
///     </para>
///     <para>
///         The registry maintains two parallel dictionaries for bidirectional lookup:
///         one mapping event names to types, and another mapping types to event names.
///     </para>
/// </remarks>
internal sealed class EventTypeRegistry : IEventTypeRegistry
{
    private readonly ConcurrentDictionary<string, Type> nameToType = new(StringComparer.Ordinal);

    private readonly ConcurrentDictionary<Type, string> typeToName = new();

    /// <inheritdoc />
    public IReadOnlyDictionary<string, Type> RegisteredTypes => nameToType;

    /// <inheritdoc />
    public void Register(
        string eventName,
        Type eventType
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        ArgumentNullException.ThrowIfNull(eventType);

        // Use TryAdd to avoid overwriting - first registration wins
        if (nameToType.TryAdd(eventName, eventType))
        {
            typeToName.TryAdd(eventType, eventName);
        }
    }

    /// <inheritdoc />
    public string? ResolveName(
        Type eventType
    )
    {
        ArgumentNullException.ThrowIfNull(eventType);
        return typeToName.TryGetValue(eventType, out string? name) ? name : null;
    }

    /// <inheritdoc />
    public Type? ResolveType(
        string eventTypeName
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventTypeName);
        return nameToType.TryGetValue(eventTypeName, out Type? eventType) ? eventType : null;
    }

    /// <inheritdoc />
    public int ScanAssembly(
        Assembly assembly
    )
    {
        ArgumentNullException.ThrowIfNull(assembly);
        int registeredCount = 0;
        foreach (Type type in assembly.GetTypes())
        {
            EventNameAttribute? attribute = type.GetCustomAttribute<EventNameAttribute>(false);
            if (attribute is null)
            {
                continue;
            }

            Register(attribute.EventName, type);
            registeredCount++;
        }

        return registeredCount;
    }
}