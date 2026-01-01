using System;
using System.Collections.Concurrent;
using System.Reflection;


namespace Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

/// <summary>
///     Provides helper methods for working with event storage names.
/// </summary>
public static class EventStorageNameHelper
{
    private static readonly ConcurrentDictionary<Type, string> StorageNameCache = new();

    /// <summary>
    ///     Gets the storage name from a type decorated with <see cref="EventStorageNameAttribute" />.
    /// </summary>
    /// <typeparam name="T">The type decorated with <see cref="EventStorageNameAttribute" />.</typeparam>
    /// <returns>The storage name string.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the type lacks an <see cref="EventStorageNameAttribute" />.</exception>
    public static string GetStorageName<T>()
        where T : class =>
        GetStorageName(typeof(T));

    /// <summary>
    ///     Gets the storage name from a type decorated with <see cref="EventStorageNameAttribute" />.
    /// </summary>
    /// <param name="type">The type decorated with <see cref="EventStorageNameAttribute" />.</param>
    /// <returns>The storage name string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type" /> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the type lacks an <see cref="EventStorageNameAttribute" />.</exception>
    public static string GetStorageName(
        Type type
    )
    {
        ArgumentNullException.ThrowIfNull(type);
        return StorageNameCache.GetOrAdd(
            type,
            static t =>
            {
                EventStorageNameAttribute? attribute = t.GetCustomAttribute<EventStorageNameAttribute>();
                if (attribute is null)
                {
                    throw new InvalidOperationException(
                        $"Type {t.Name} does not have an EventStorageNameAttribute. " +
                        $"Decorate the type with [EventStorageName(\"APP\", \"MODULE\", \"NAME\")] to define its storage identity.");
                }

                return attribute.StorageName;
            });
    }

    /// <summary>
    ///     Tries to get the storage name from a type decorated with <see cref="EventStorageNameAttribute" />.
    /// </summary>
    /// <typeparam name="T">The type potentially decorated with <see cref="EventStorageNameAttribute" />.</typeparam>
    /// <param name="storageName">When this method returns, contains the storage name if found; otherwise, null.</param>
    /// <returns><c>true</c> if the storage name was found; otherwise, <c>false</c>.</returns>
    public static bool TryGetStorageName<T>(
        out string? storageName
    )
        where T : class =>
        TryGetStorageName(typeof(T), out storageName);

    /// <summary>
    ///     Tries to get the storage name from a type decorated with <see cref="EventStorageNameAttribute" />.
    /// </summary>
    /// <param name="type">The type potentially decorated with <see cref="EventStorageNameAttribute" />.</param>
    /// <param name="storageName">When this method returns, contains the storage name if found; otherwise, null.</param>
    /// <returns><c>true</c> if the storage name was found; otherwise, <c>false</c>.</returns>
    public static bool TryGetStorageName(
        Type type,
        out string? storageName
    )
    {
        ArgumentNullException.ThrowIfNull(type);
        EventStorageNameAttribute? attribute = type.GetCustomAttribute<EventStorageNameAttribute>();
        if (attribute is null)
        {
            storageName = null;
            return false;
        }

        storageName = attribute.StorageName;
        return true;
    }
}