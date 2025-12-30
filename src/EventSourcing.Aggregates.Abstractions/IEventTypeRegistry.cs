using System;
using System.Collections.Generic;
using System.Reflection;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;


namespace Mississippi.EventSourcing.Aggregates.Abstractions;

/// <summary>
///     Provides bidirectional resolution between event types and their string names.
/// </summary>
/// <remarks>
///     <para>
///         The event type registry is built at application startup and provides O(1) lookups
///         for both name-to-type and type-to-name resolution. This enables efficient
///         deserialization during aggregate hydration and serialization when persisting events.
///     </para>
///     <para>
///         Event types are identified by their <see cref="EventNameAttribute" />, which provides
///         a stable string identity that survives type renames and namespace changes.
///     </para>
/// </remarks>
public interface IEventTypeRegistry
{
    /// <summary>
    ///     Gets all registered event types.
    /// </summary>
    IReadOnlyDictionary<string, Type> RegisteredTypes { get; }

    /// <summary>
    ///     Registers an event type with its corresponding event name.
    /// </summary>
    /// <param name="eventName">The event name as defined by <see cref="EventNameAttribute" />.</param>
    /// <param name="eventType">The CLR type of the event.</param>
    void Register(
        string eventName,
        Type eventType
    );

    /// <summary>
    ///     Resolves the event name for a CLR type.
    /// </summary>
    /// <param name="eventType">The CLR type of the event.</param>
    /// <returns>The event name, or null if the type is not registered.</returns>
    string? ResolveName(
        Type eventType
    );

    /// <summary>
    ///     Resolves an event type from its string name.
    /// </summary>
    /// <param name="eventTypeName">The event type name to resolve.</param>
    /// <returns>The CLR type of the event, or null if not found.</returns>
    Type? ResolveType(
        string eventTypeName
    );

    /// <summary>
    ///     Scans an assembly for types decorated with <see cref="EventNameAttribute" /> and registers them.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <returns>The number of event types registered from the assembly.</returns>
    int ScanAssembly(
        Assembly assembly
    );
}