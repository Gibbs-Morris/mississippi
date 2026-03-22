using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

using Orleans;


namespace Mississippi.Brooks.Abstractions.Attributes;

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
    /// <remarks>
    ///     Constructed generic types use the declared storage name as a base and append a deterministic hash
    ///     derived from the generic type definition identity plus the generic argument identities.
    ///     Non-generic types return the declared storage name unchanged.
    /// </remarks>
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
                EventStorageNameAttribute? attribute = GetAttribute(t);
                if (attribute is null)
                {
                    throw new InvalidOperationException(
                        $"Type {t.Name} does not have an EventStorageNameAttribute. " +
                        $"Decorate the type with [EventStorageName(\"APP\", \"MODULE\", \"NAME\", version: 1)] to define its storage identity.");
                }

                return BuildStorageName(t, attribute);
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
    /// <remarks>
    ///     Constructed generic types use the declared storage name as a base and append a deterministic hash
    ///     derived from the generic type definition identity plus the generic argument identities.
    ///     Non-generic types return the declared storage name unchanged.
    /// </remarks>
    /// <returns><c>true</c> if the storage name was found; otherwise, <c>false</c>.</returns>
    public static bool TryGetStorageName(
        Type type,
        out string? storageName
    )
    {
        ArgumentNullException.ThrowIfNull(type);
        EventStorageNameAttribute? attribute = GetAttribute(type);
        if (attribute is null)
        {
            storageName = null;
            return false;
        }

        storageName = BuildStorageName(type, attribute);
        return true;
    }

    private static string BuildStorageName(
        Type type,
        EventStorageNameAttribute attribute
    )
    {
        if (!type.IsConstructedGenericType)
        {
            return attribute.StorageName;
        }

        string genericTypeHash = GetGenericTypeHash(type);
        return $"{attribute.AppName}.{attribute.ModuleName}.{attribute.Name}G{genericTypeHash}.V{attribute.Version}";
    }

    private static EventStorageNameAttribute? GetAttribute(
        Type type
    ) =>
        type.GetCustomAttribute<EventStorageNameAttribute>() ??
        (type.IsConstructedGenericType
            ? type.GetGenericTypeDefinition().GetCustomAttribute<EventStorageNameAttribute>()
            : null);

    private static string GetGenericTypeHash(
        Type type
    )
    {
        string identity = string.Join("|", type.GetGenericArguments().Select(GetTypeIdentity));
        byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(identity));
        return Convert.ToHexString(hashBytes[..16]);
    }

    private static string? GetTypeAlias(
        Type type
    ) =>
        type.GetCustomAttribute<AliasAttribute>(false)?.Alias ??
        (type.IsConstructedGenericType
            ? type.GetGenericTypeDefinition().GetCustomAttribute<AliasAttribute>(false)?.Alias
            : null);

    private static string GetTypeIdentity(
        Type type
    )
    {
        if (!type.IsConstructedGenericType)
        {
            return GetTypeAlias(type) ?? type.FullName ?? type.Name;
        }

        Type genericDefinition = type.GetGenericTypeDefinition();
        string baseIdentity = GetTypeAlias(genericDefinition) ?? genericDefinition.FullName ?? genericDefinition.Name;
        string genericArguments = string.Join(",", type.GetGenericArguments().Select(GetTypeIdentity));
        return $"{baseIdentity}[{genericArguments}]";
    }
}