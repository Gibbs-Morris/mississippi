using System;

using Orleans;


namespace Mississippi.EventSourcing.Brooks.Abstractions;

/// <summary>
///     Represents a composite key for identifying brooks, consisting of a brook name and entity id component.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.Brooks.Abstractions.BrookKey")]
public readonly record struct BrookKey
{
    private const int MaxLength = 4192;

    private const char Separator = '|';

    /// <summary>
    ///     Initializes a new instance of the <see cref="BrookKey" /> struct.
    /// </summary>
    /// <param name="brookName">The brook name component of the key.</param>
    /// <param name="entityId">The entity id component of the key.</param>
    /// <exception cref="ArgumentNullException">Thrown when brookName or entityId is null.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when the composite key exceeds the maximum length or contains invalid
    ///     characters.
    /// </exception>
    public BrookKey(
        string brookName,
        string entityId
    )
    {
        ValidateComponent(brookName, nameof(brookName));
        ValidateComponent(entityId, nameof(entityId));
        if ((brookName.Length + entityId.Length + 1) > MaxLength)
        {
            throw new ArgumentException($"Composite key exceeds the {MaxLength}-character limit.");
        }

        BrookName = brookName;
        EntityId = entityId;
    }

    /// <summary>
    ///     Gets the brook name component of the brook key.
    /// </summary>
    [Id(0)]
    public string BrookName { get; }

    /// <summary>
    ///     Gets the entity id component of the brook key.
    /// </summary>
    [Id(1)]
    public string EntityId { get; }

    /// <summary>
    ///     Creates a brook key from a grain type decorated with <see cref="Attributes.BrookNameAttribute" />
    ///     and an entity identifier.
    /// </summary>
    /// <typeparam name="TGrain">
    ///     The grain type decorated with <see cref="Attributes.BrookNameAttribute" />.
    /// </typeparam>
    /// <param name="entityId">The unique identifier for the entity within the brook.</param>
    /// <returns>A brook key constructed from the grain's brook name and entity identifier.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when <typeparamref name="TGrain" /> is not decorated with
    ///     <see cref="Attributes.BrookNameAttribute" />.
    /// </exception>
    public static BrookKey ForGrain<TGrain>(
        string entityId
    )
        where TGrain : class =>
        new(BrookNameHelper.GetBrookName<TGrain>(), entityId);

    /// <summary>
    ///     Creates a brook key from any type decorated with <see cref="Attributes.BrookNameAttribute" />
    ///     and an entity identifier.
    /// </summary>
    /// <typeparam name="T">
    ///     The type decorated with <see cref="Attributes.BrookNameAttribute" />.
    ///     This can be a projection type, grain type, or any other type with the attribute.
    /// </typeparam>
    /// <param name="entityId">The unique identifier for the entity within the brook.</param>
    /// <returns>A brook key constructed from the type's brook name and entity identifier.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when <typeparamref name="T" /> is not decorated with
    ///     <see cref="Attributes.BrookNameAttribute" />.
    /// </exception>
    public static BrookKey ForType<T>(
        string entityId
    )
        where T : class =>
        new(BrookNameHelper.GetBrookName<T>(), entityId);

    /// <summary>
    ///     Converts a brook key to its string representation.
    /// </summary>
    /// <param name="key">The brook key to convert.</param>
    /// <returns>A string representation of the brook key in the format "brookName|entityId".</returns>
    public static string FromBrookKey(
        BrookKey key
    ) =>
        key;

    /// <summary>
    ///     Creates a brook key from its string representation.
    /// </summary>
    /// <param name="value">The string value to convert.</param>
    /// <returns>A brook key parsed from the string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="FormatException">Thrown when the string is not in the correct format.</exception>
    public static BrookKey FromString(
        string value
    )
    {
        ArgumentNullException.ThrowIfNull(value);
        int idx = value.IndexOf(Separator, StringComparison.Ordinal);
        if (idx < 0)
        {
            throw new FormatException($"Composite key must be in the form '<brookName>{Separator}<entityId>'.");
        }

        string brookName = value[..idx];
        string entityId = value[(idx + 1)..];
        return new(brookName, entityId);
    }

    /// <summary>
    ///     Implicitly converts a <see cref="BrookKey" /> to its string representation.
    /// </summary>
    /// <param name="key">The brook key to convert.</param>
    /// <returns>A string representation of the brook key in the format "brookName|entityId".</returns>
    public static implicit operator string(
        BrookKey key
    ) =>
        $"{key.BrookName}{Separator}{key.EntityId}";

    private static void ValidateComponent(
        string value,
        string paramName
    )
    {
        if (value is null)
        {
            throw new ArgumentNullException(paramName);
        }

        if (value.Contains(Separator, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Value cannot contain the separator character '{Separator}'.", paramName);
        }
    }

    /// <summary>
    ///     Returns the string representation of this brook key.
    /// </summary>
    /// <returns>A string representation of the brook key in the format "brookName|entityId".</returns>
    public override string ToString() => this;
}