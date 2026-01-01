using System;
using System.Reflection;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;


namespace Mississippi.EventSourcing.Brooks.Abstractions;

/// <summary>
///     Provides helper methods for working with <see cref="IBrookDefinition" /> types
///     and types decorated with <see cref="BrookNameAttribute" />.
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
    ///     Gets the brook name from a grain type.
    ///     The attribute MUST be declared directly on the concrete type (not inherited).
    /// </summary>
    /// <param name="grainType">The grain type to read the attribute from.</param>
    /// <returns>The brook name string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="grainType" /> is null.</exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the attribute is not found on the concrete type.
    /// </exception>
    /// <remarks>
    ///     This is a convenience method that calls <see cref="GetDefinition" /> and returns
    ///     the <see cref="BrookNameAttribute.BrookName" /> property.
    /// </remarks>
    public static string GetBrookNameFromGrain(
        Type grainType
    ) =>
        GetDefinition(grainType).BrookName;

    /// <summary>
    ///     Gets the <see cref="BrookNameAttribute" /> from a grain type.
    ///     The attribute MUST be declared directly on the concrete type (not inherited).
    /// </summary>
    /// <param name="grainType">The grain type to read the attribute from.</param>
    /// <returns>The <see cref="BrookNameAttribute" /> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="grainType" /> is null.</exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the attribute is not found on the concrete type.
    /// </exception>
    /// <remarks>
    ///     <para>
    ///         This method uses <c>inherit: false</c> to ensure the attribute is declared
    ///         directly on the final sealed grain class, not on a base class.
    ///     </para>
    ///     <para>
    ///         Use this method for grain base classes that need to read their brook identity
    ///         at runtime from the concrete derived class.
    ///     </para>
    /// </remarks>
    public static BrookNameAttribute GetDefinition(
        Type grainType
    )
    {
        ArgumentNullException.ThrowIfNull(grainType);
        BrookNameAttribute? attribute = grainType.GetCustomAttribute<BrookNameAttribute>(false);
        if (attribute is null)
        {
            throw new InvalidOperationException(
                $"Type '{grainType.FullName}' is missing [BrookName] attribute. " +
                $"The attribute MUST be declared directly on the final sealed class, not on a base class. " +
                $"Add [BrookName(\"APP\", \"MODULE\", \"NAME\")] to '{grainType.Name}'.");
        }

        return attribute;
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