using System;
using System.Collections.Concurrent;
using System.Reflection;


namespace Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

/// <summary>
///     Provides helper methods for working with event type names.
/// </summary>
public static class EventNameHelper
{
    private static readonly ConcurrentDictionary<Type, string> EventNameCache = new();

    /// <summary>
    ///     Gets the event name from a type decorated with <see cref="EventNameAttribute" />.
    /// </summary>
    /// <typeparam name="T">The type decorated with <see cref="EventNameAttribute" />.</typeparam>
    /// <returns>The event name string.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the type lacks an <see cref="EventNameAttribute" />.</exception>
    public static string GetEventName<T>()
        where T : class =>
        GetEventName(typeof(T));

    /// <summary>
    ///     Gets the event name from a type decorated with <see cref="EventNameAttribute" />.
    /// </summary>
    /// <param name="type">The type decorated with <see cref="EventNameAttribute" />.</param>
    /// <returns>The event name string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type" /> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the type lacks an <see cref="EventNameAttribute" />.</exception>
    public static string GetEventName(
        Type type
    )
    {
        ArgumentNullException.ThrowIfNull(type);
        return EventNameCache.GetOrAdd(
            type,
            static t =>
            {
                EventNameAttribute? attribute = t.GetCustomAttribute<EventNameAttribute>();
                if (attribute is null)
                {
                    throw new InvalidOperationException(
                        $"Type {t.Name} does not have an EventNameAttribute. " +
                        $"Decorate the type with [EventName(\"APP\", \"MODULE\", \"NAME\")] to define its event identity.");
                }

                return attribute.EventName;
            });
    }

    /// <summary>
    ///     Tries to get the event name from a type decorated with <see cref="EventNameAttribute" />.
    /// </summary>
    /// <typeparam name="T">The type potentially decorated with <see cref="EventNameAttribute" />.</typeparam>
    /// <param name="eventName">When this method returns, contains the event name if found; otherwise, null.</param>
    /// <returns><c>true</c> if the event name was found; otherwise, <c>false</c>.</returns>
    public static bool TryGetEventName<T>(
        out string? eventName
    )
        where T : class =>
        TryGetEventName(typeof(T), out eventName);

    /// <summary>
    ///     Tries to get the event name from a type decorated with <see cref="EventNameAttribute" />.
    /// </summary>
    /// <param name="type">The type potentially decorated with <see cref="EventNameAttribute" />.</param>
    /// <param name="eventName">When this method returns, contains the event name if found; otherwise, null.</param>
    /// <returns><c>true</c> if the event name was found; otherwise, <c>false</c>.</returns>
    public static bool TryGetEventName(
        Type type,
        out string? eventName
    )
    {
        ArgumentNullException.ThrowIfNull(type);
        EventNameAttribute? attribute = type.GetCustomAttribute<EventNameAttribute>();
        if (attribute is null)
        {
            eventName = null;
            return false;
        }

        eventName = attribute.EventName;
        return true;
    }
}