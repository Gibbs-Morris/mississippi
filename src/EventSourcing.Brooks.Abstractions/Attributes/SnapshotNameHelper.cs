using System;
using System.Collections.Concurrent;
using System.Reflection;


namespace Mississippi.EventSourcing.Abstractions.Attributes;

/// <summary>
///     Provides helper methods for working with snapshot type names.
/// </summary>
public static class SnapshotNameHelper
{
    private static readonly ConcurrentDictionary<Type, string> SnapshotNameCache = new();

    /// <summary>
    ///     Gets the snapshot name from a type decorated with <see cref="SnapshotNameAttribute" />.
    /// </summary>
    /// <typeparam name="T">The type decorated with <see cref="SnapshotNameAttribute" />.</typeparam>
    /// <returns>The snapshot name string.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the type lacks a <see cref="SnapshotNameAttribute" />.</exception>
    public static string GetSnapshotName<T>()
        where T : class =>
        GetSnapshotName(typeof(T));

    /// <summary>
    ///     Gets the snapshot name from a type decorated with <see cref="SnapshotNameAttribute" />.
    /// </summary>
    /// <param name="type">The type decorated with <see cref="SnapshotNameAttribute" />.</param>
    /// <returns>The snapshot name string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type" /> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the type lacks a <see cref="SnapshotNameAttribute" />.</exception>
    public static string GetSnapshotName(
        Type type
    )
    {
        ArgumentNullException.ThrowIfNull(type);
        return SnapshotNameCache.GetOrAdd(
            type,
            static t =>
            {
                SnapshotNameAttribute? attribute = t.GetCustomAttribute<SnapshotNameAttribute>();
                if (attribute is null)
                {
                    throw new InvalidOperationException(
                        $"Type {t.Name} does not have a SnapshotNameAttribute. " +
                        $"Decorate the type with [SnapshotName(\"APP\", \"MODULE\", \"NAME\")] to define its snapshot identity.");
                }

                return attribute.SnapshotName;
            });
    }

    /// <summary>
    ///     Tries to get the snapshot name from a type decorated with <see cref="SnapshotNameAttribute" />.
    /// </summary>
    /// <typeparam name="T">The type potentially decorated with <see cref="SnapshotNameAttribute" />.</typeparam>
    /// <param name="snapshotName">When this method returns, contains the snapshot name if found; otherwise, null.</param>
    /// <returns><c>true</c> if the snapshot name was found; otherwise, <c>false</c>.</returns>
    public static bool TryGetSnapshotName<T>(
        out string? snapshotName
    )
        where T : class =>
        TryGetSnapshotName(typeof(T), out snapshotName);

    /// <summary>
    ///     Tries to get the snapshot name from a type decorated with <see cref="SnapshotNameAttribute" />.
    /// </summary>
    /// <param name="type">The type potentially decorated with <see cref="SnapshotNameAttribute" />.</param>
    /// <param name="snapshotName">When this method returns, contains the snapshot name if found; otherwise, null.</param>
    /// <returns><c>true</c> if the snapshot name was found; otherwise, <c>false</c>.</returns>
    public static bool TryGetSnapshotName(
        Type type,
        out string? snapshotName
    )
    {
        ArgumentNullException.ThrowIfNull(type);
        SnapshotNameAttribute? attribute = type.GetCustomAttribute<SnapshotNameAttribute>();
        if (attribute is null)
        {
            snapshotName = null;
            return false;
        }

        snapshotName = attribute.SnapshotName;
        return true;
    }
}