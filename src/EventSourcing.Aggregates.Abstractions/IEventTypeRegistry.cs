using System;

using Mississippi.EventSourcing.Abstractions.Attributes;


namespace Mississippi.EventSourcing.Aggregates.Abstractions;

/// <summary>
///     Provides resolution of event types from their string names.
/// </summary>
/// <remarks>
///     The event type registry is used during aggregate hydration to deserialize
///     events from the brook. Event types should be registered during application startup.
/// </remarks>
public interface IEventTypeRegistry
{
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
    ///     Resolves an event type from its string name.
    /// </summary>
    /// <param name="eventTypeName">The event type name to resolve.</param>
    /// <returns>The CLR type of the event, or null if not found.</returns>
    Type? ResolveType(
        string eventTypeName
    );
}