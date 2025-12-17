using System;
using System.Reflection;

using Mississippi.EventSourcing.Abstractions.Attributes;


namespace Mississippi.EventSourcing.Abstractions;

/// <summary>
///     Provides helper methods for working with <see cref="IBrookDefinition" /> types.
/// </summary>
public static class BrookDefinitionHelper
{
    /// <summary>
    ///     Gets the brook name from a type decorated with <see cref="BrookNameAttribute" />.
    /// </summary>
    /// <typeparam name="T">The type decorated with <see cref="BrookNameAttribute" />.</typeparam>
    /// <returns>The brook name string.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the type lacks a <see cref="BrookNameAttribute" />.</exception>
    public static string GetBrookName<T>()
        where T : class =>
        GetBrookName(typeof(T));

    /// <summary>
    ///     Gets the brook name from a type decorated with <see cref="BrookNameAttribute" />.
    /// </summary>
    /// <param name="type">The type decorated with <see cref="BrookNameAttribute" />.</param>
    /// <returns>The brook name string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type" /> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the type lacks a <see cref="BrookNameAttribute" />.</exception>
    public static string GetBrookName(
        Type type
    )
    {
        ArgumentNullException.ThrowIfNull(type);
        BrookNameAttribute? attribute = type.GetCustomAttribute<BrookNameAttribute>();
        if (attribute is null)
        {
            throw new InvalidOperationException(
                $"Type {type.Name} does not have a BrookNameAttribute. " +
                $"Decorate the type with [BrookName(\"APP\", \"MODULE\", \"NAME\")] to define its brook identity.");
        }

        return attribute.BrookName;
    }

    /// <summary>
    ///     Tries to get the brook name from a type decorated with <see cref="BrookNameAttribute" />.
    /// </summary>
    /// <typeparam name="T">The type potentially decorated with <see cref="BrookNameAttribute" />.</typeparam>
    /// <param name="brookName">When this method returns, contains the brook name if found; otherwise, null.</param>
    /// <returns><c>true</c> if the brook name was found; otherwise, <c>false</c>.</returns>
    public static bool TryGetBrookName<T>(
        out string? brookName
    )
        where T : class =>
        TryGetBrookName(typeof(T), out brookName);

    /// <summary>
    ///     Tries to get the brook name from a type decorated with <see cref="BrookNameAttribute" />.
    /// </summary>
    /// <param name="type">The type potentially decorated with <see cref="BrookNameAttribute" />.</param>
    /// <param name="brookName">When this method returns, contains the brook name if found; otherwise, null.</param>
    /// <returns><c>true</c> if the brook name was found; otherwise, <c>false</c>.</returns>
    public static bool TryGetBrookName(
        Type type,
        out string? brookName
    )
    {
        ArgumentNullException.ThrowIfNull(type);
        BrookNameAttribute? attribute = type.GetCustomAttribute<BrookNameAttribute>();
        if (attribute is null)
        {
            brookName = null;
            return false;
        }

        brookName = attribute.BrookName;
        return true;
    }
}