using System;

using Mississippi.EventSourcing.Abstractions;

using Orleans;


namespace Mississippi.EventSourcing.UxProjections.Abstractions;

/// <summary>
///     Represents a composite key for identifying UX projections, consisting of a projection type name and brook key.
/// </summary>
/// <remarks>
///     <para>
///         UX projections are keyed by both the projection type and the brook they consume.
///         This allows multiple different projection types to consume the same brook independently,
///         each maintaining its own cursor and cached state.
///     </para>
///     <para>
///         The string format is "{projectionTypeName}|{brookKey}" where brookKey itself is "{brookType}|{brookId}".
///     </para>
/// </remarks>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.UxProjections.Abstractions.UxProjectionKey")]
public readonly record struct UxProjectionKey
{
    private const int MaxLength = 2048;

    private const char Separator = '|';

    /// <summary>
    ///     Initializes a new instance of the <see cref="UxProjectionKey" /> struct.
    /// </summary>
    /// <param name="projectionTypeName">The name of the projection type.</param>
    /// <param name="brookKey">The brook key identifying the source event stream.</param>
    /// <exception cref="ArgumentNullException">Thrown when projectionTypeName is null.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when the composite key exceeds the maximum length or contains invalid characters.
    /// </exception>
    public UxProjectionKey(
        string projectionTypeName,
        BrookKey brookKey
    )
    {
        ValidateProjectionTypeName(projectionTypeName);
        string brookKeyString = brookKey;
        if ((projectionTypeName.Length + brookKeyString.Length + 1) > MaxLength)
        {
            throw new ArgumentException($"Composite key exceeds the {MaxLength}-character limit.");
        }

        ProjectionTypeName = projectionTypeName;
        BrookKey = brookKey;
    }

    /// <summary>
    ///     Gets the brook key identifying the source event stream.
    /// </summary>
    [Id(1)]
    public BrookKey BrookKey { get; }

    /// <summary>
    ///     Gets the name of the projection type.
    /// </summary>
    [Id(0)]
    public string ProjectionTypeName { get; }

    /// <summary>
    ///     Creates a UX projection key for a specific projection type and brook.
    /// </summary>
    /// <typeparam name="TProjection">The projection type.</typeparam>
    /// <typeparam name="TBrook">The brook definition type.</typeparam>
    /// <param name="entityId">The entity identifier within the brook.</param>
    /// <returns>A UX projection key for the specified projection and brook.</returns>
    public static UxProjectionKey For<TProjection, TBrook>(
        string entityId
    )
        where TBrook : IBrookDefinition =>
        new(typeof(TProjection).Name, BrookKey.For<TBrook>(entityId));

    /// <summary>
    ///     Converts a UX projection key to its string representation.
    /// </summary>
    /// <param name="key">The UX projection key to convert.</param>
    /// <returns>A string representation of the key in the format "projectionTypeName|brookType|brookId".</returns>
    public static string FromUxProjectionKey(
        UxProjectionKey key
    ) =>
        key;

    /// <summary>
    ///     Creates a UX projection key from its string representation.
    /// </summary>
    /// <param name="value">The string value to convert.</param>
    /// <returns>A UX projection key parsed from the string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="FormatException">Thrown when the string is not in the correct format.</exception>
    public static UxProjectionKey FromString(
        string value
    )
    {
        ArgumentNullException.ThrowIfNull(value);
        int firstSeparator = value.IndexOf(Separator, StringComparison.Ordinal);
        if (firstSeparator < 0)
        {
            throw new FormatException(
                $"UX projection key must be in the form '<projectionTypeName>{Separator}<brookType>{Separator}<brookId>'.");
        }

        string projectionTypeName = value[..firstSeparator];
        string brookKeyPart = value[(firstSeparator + 1)..];

        // Parse the brook key from the remaining part
        BrookKey brookKey = BrookKey.FromString(brookKeyPart);

        return new(projectionTypeName, brookKey);
    }

    /// <summary>
    ///     Implicitly converts a <see cref="UxProjectionKey" /> to its string representation.
    /// </summary>
    /// <param name="key">The UX projection key to convert.</param>
    /// <returns>A string representation in the format "projectionTypeName|brookType|brookId".</returns>
    public static implicit operator string(
        UxProjectionKey key
    ) =>
        $"{key.ProjectionTypeName}{Separator}{(string)key.BrookKey}";

    /// <summary>
    ///     Implicitly converts a string to a <see cref="UxProjectionKey" />.
    /// </summary>
    /// <param name="value">The string value to convert.</param>
    /// <returns>A UX projection key parsed from the string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="FormatException">Thrown when the string is not in the correct format.</exception>
    public static implicit operator UxProjectionKey(
        string value
    ) =>
        FromString(value);

    /// <summary>
    ///     Returns the string representation of this UX projection key.
    /// </summary>
    /// <returns>A string representation in the format "projectionTypeName|brookType|brookId".</returns>
    public override string ToString() => this;

    private static void ValidateProjectionTypeName(
        string value
    )
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Contains(Separator, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Projection type name cannot contain the separator character '{Separator}'.",
                nameof(value));
        }
    }
}
