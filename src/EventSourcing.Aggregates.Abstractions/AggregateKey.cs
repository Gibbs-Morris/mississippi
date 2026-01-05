using System;

using Mississippi.EventSourcing.Brooks.Abstractions;

using Orleans;


namespace Mississippi.EventSourcing.Aggregates.Abstractions;

/// <summary>
///     Represents a composite key for identifying aggregates, consisting of an aggregate type name and entity ID.
/// </summary>
/// <remarks>
///     <para>
///         This type provides a domain-level abstraction over <see cref="BrookKey" />, allowing application code
///         to work with aggregate keys without directly depending on the Brooks (event stream) infrastructure.
///     </para>
///     <para>
///         The key format is compatible with <see cref="BrookKey" /> and can be implicitly converted for use
///         with framework internals that require brook keys.
///     </para>
/// </remarks>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.Aggregates.Abstractions.AggregateKey")]
public readonly record struct AggregateKey
{
    private const int MaxLength = 1024;

    private const char Separator = '|';

    /// <summary>
    ///     Initializes a new instance of the <see cref="AggregateKey" /> struct.
    /// </summary>
    /// <param name="aggregateTypeName">The aggregate type name component of the key.</param>
    /// <param name="entityId">The entity ID component of the key.</param>
    /// <exception cref="ArgumentNullException">Thrown when aggregateTypeName or entityId is null.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when the composite key exceeds the maximum length or contains invalid characters.
    /// </exception>
    public AggregateKey(
        string aggregateTypeName,
        string entityId
    )
    {
        ValidateComponent(aggregateTypeName, nameof(aggregateTypeName));
        ValidateComponent(entityId, nameof(entityId));
        if ((aggregateTypeName.Length + entityId.Length + 1) > MaxLength)
        {
            throw new ArgumentException($"Composite key exceeds the {MaxLength}-character limit.");
        }

        AggregateTypeName = aggregateTypeName;
        EntityId = entityId;
    }

    /// <summary>
    ///     Gets the aggregate type name component of the key.
    /// </summary>
    /// <remarks>
    ///     This corresponds to the brook name used by the underlying event stream.
    /// </remarks>
    [Id(0)]
    public string AggregateTypeName { get; }

    /// <summary>
    ///     Gets the entity ID component of the key.
    /// </summary>
    [Id(1)]
    public string EntityId { get; }

    /// <summary>
    ///     Creates an aggregate key for the specified grain type and entity identifier.
    /// </summary>
    /// <typeparam name="TGrain">
    ///     The aggregate grain interface type, which must be decorated with a brook name attribute.
    /// </typeparam>
    /// <param name="entityId">The unique identifier for the entity.</param>
    /// <returns>An aggregate key for the specified grain type and entity.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when <typeparamref name="TGrain" /> is not decorated with a brook name attribute.
    /// </exception>
    public static AggregateKey ForAggregate<TGrain>(
        string entityId
    )
        where TGrain : class
    {
        BrookKey brookKey = BrookKey.ForGrain<TGrain>(entityId);
        return new(brookKey.BrookName, brookKey.EntityId);
    }

    /// <summary>
    ///     Creates an aggregate key from its string representation.
    /// </summary>
    /// <param name="value">The string value to convert.</param>
    /// <returns>An aggregate key parsed from the string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="FormatException">Thrown when the string is not in the correct format.</exception>
    public static AggregateKey FromString(
        string value
    )
    {
        ArgumentNullException.ThrowIfNull(value);
        int idx = value.IndexOf(Separator, StringComparison.Ordinal);
        if (idx < 0)
        {
            throw new FormatException($"Aggregate key must be in the form '<aggregateTypeName>{Separator}<entityId>'.");
        }

        string aggregateTypeName = value[..idx];
        string entityId = value[(idx + 1)..];
        return new(aggregateTypeName, entityId);
    }

    /// <summary>
    ///     Implicitly converts an <see cref="AggregateKey" /> to a <see cref="BrookKey" />.
    /// </summary>
    /// <param name="key">The aggregate key to convert.</param>
    /// <returns>A <see cref="BrookKey" /> with the same aggregate type name and entity ID.</returns>
    public static implicit operator BrookKey(
        AggregateKey key
    ) =>
        key.ToBrookKey();

    /// <summary>
    ///     Implicitly converts an <see cref="AggregateKey" /> to its string representation.
    /// </summary>
    /// <param name="key">The aggregate key to convert.</param>
    /// <returns>A string representation of the aggregate key in the format "aggregateTypeName|entityId".</returns>
    public static implicit operator string(
        AggregateKey key
    ) =>
        $"{key.AggregateTypeName}{Separator}{key.EntityId}";

    /// <summary>
    ///     Implicitly converts a string to an <see cref="AggregateKey" />.
    /// </summary>
    /// <param name="value">The string value to convert.</param>
    /// <returns>An aggregate key parsed from the string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="FormatException">Thrown when the string is not in the correct format.</exception>
    public static implicit operator AggregateKey(
        string value
    ) =>
        FromString(value);

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
    ///     Converts the aggregate key to its underlying <see cref="BrookKey" /> representation.
    /// </summary>
    /// <returns>A <see cref="BrookKey" /> with the same aggregate type name and entity ID.</returns>
    public BrookKey ToBrookKey() => new(AggregateTypeName, EntityId);

    /// <summary>
    ///     Returns the string representation of this aggregate key.
    /// </summary>
    /// <returns>A string representation of the aggregate key in the format "aggregateTypeName|entityId".</returns>
    public override string ToString() => this;
}