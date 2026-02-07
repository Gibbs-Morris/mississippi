using System;

using Orleans;


namespace Mississippi.EventSourcing.Aggregates.Abstractions;

/// <summary>
///     Represents a strongly-typed key for identifying aggregates by entity ID.
/// </summary>
/// <remarks>
///     <para>
///         Aggregate grains are keyed by entity ID only. The brook name is derived
///         from the aggregate type parameter's <c>[BrookName]</c> attribute at runtime.
///     </para>
///     <para>
///         This key provides type safety while maintaining a simple string representation.
///     </para>
/// </remarks>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.Aggregates.Abstractions.AggregateKey")]
public readonly record struct AggregateKey
{
    private const int MaxLength = 4192;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AggregateKey" /> struct.
    /// </summary>
    /// <param name="entityId">The entity identifier.</param>
    /// <exception cref="ArgumentNullException">Thrown when entityId is null.</exception>
    /// <exception cref="ArgumentException">Thrown when entityId exceeds the maximum length.</exception>
    public AggregateKey(
        string entityId
    )
    {
        ArgumentNullException.ThrowIfNull(entityId);
        if (entityId.Length > MaxLength)
        {
            throw new ArgumentException($"Entity ID exceeds the {MaxLength}-character limit.", nameof(entityId));
        }

        EntityId = entityId;
    }

    /// <summary>
    ///     Gets the entity identifier.
    /// </summary>
    [Id(0)]
    public string EntityId { get => field ?? string.Empty; init; }

    /// <summary>
    ///     Creates an <see cref="AggregateKey" /> from its string representation.
    ///     This method satisfies CA2225 as an alternate for the implicit string conversion operator.
    /// </summary>
    /// <param name="entityId">The entity ID string to convert.</param>
    /// <returns>A new <see cref="AggregateKey" /> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when entityId is null.</exception>
    public static AggregateKey FromString(
        string entityId
    ) =>
        Parse(entityId);

    /// <summary>
    ///     Parses a string into an <see cref="AggregateKey" />.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <returns>A new <see cref="AggregateKey" /> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    public static AggregateKey Parse(
        string value
    )
    {
        ArgumentNullException.ThrowIfNull(value);
        return new(value);
    }

    /// <summary>
    ///     Implicitly converts an <see cref="AggregateKey" /> to its string representation.
    /// </summary>
    /// <param name="key">The aggregate key to convert.</param>
    /// <returns>The entity ID string.</returns>
    public static implicit operator string(
        AggregateKey key
    ) =>
        key.EntityId;

    /// <summary>
    ///     Implicitly converts a string to an <see cref="AggregateKey" />.
    /// </summary>
    /// <param name="entityId">The entity ID string.</param>
    /// <returns>A new <see cref="AggregateKey" /> instance.</returns>
    public static implicit operator AggregateKey(
        string entityId
    ) =>
        new(entityId);

    /// <summary>
    ///     Returns the string representation of this key.
    /// </summary>
    /// <returns>The entity ID.</returns>
    public override string ToString() => EntityId;
}