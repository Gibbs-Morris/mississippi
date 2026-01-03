using System;
using System.Globalization;

using Mississippi.EventSourcing.Brooks.Abstractions;

using Orleans;


namespace Mississippi.EventSourcing.UxProjections.Abstractions;

/// <summary>
///     Represents a composite key for identifying a versioned UX projection,
///     consisting of a projection key and a specific version.
/// </summary>
/// <remarks>
///     <para>
///         Versioned UX projection keys extend <see cref="UxProjectionKey" /> with a version
///         to enable caching of specific projection versions. This allows multiple clients
///         requesting the same version to share a cached grain instance.
///     </para>
///     <para>
///         The string format is "{projectionTypeName}|{brookType}|{brookId}|{version}".
///     </para>
/// </remarks>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.UxProjections.Abstractions.UxProjectionVersionedKey")]
public readonly record struct UxProjectionVersionedKey
{
    private const int MaxLength = 2048;

    private const char Separator = '|';

    /// <summary>
    ///     Initializes a new instance of the <see cref="UxProjectionVersionedKey" /> struct.
    /// </summary>
    /// <param name="projectionKey">The base projection key.</param>
    /// <param name="version">The specific version of the projection.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when version is negative.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when the composite key exceeds the maximum length.
    /// </exception>
    public UxProjectionVersionedKey(
        UxProjectionKey projectionKey,
        BrookPosition version
    )
    {
        if (version.NotSet)
        {
            throw new ArgumentOutOfRangeException(nameof(version), "Version cannot be NotSet (-1).");
        }

        string projectionKeyString = projectionKey;
        string versionString = version.Value.ToString(CultureInfo.InvariantCulture);
        if ((projectionKeyString.Length + versionString.Length + 1) > MaxLength)
        {
            throw new ArgumentException($"Composite key exceeds the {MaxLength}-character limit.");
        }

        ProjectionKey = projectionKey;
        Version = version;
    }

    /// <summary>
    ///     Gets the base projection key.
    /// </summary>
    [Id(0)]
    public UxProjectionKey ProjectionKey { get; }

    /// <summary>
    ///     Gets the specific version of the projection.
    /// </summary>
    [Id(1)]
    public BrookPosition Version { get; }

    /// <summary>
    ///     Creates a versioned UX projection key for a specific projection type, grain, and version.
    /// </summary>
    /// <typeparam name="TProjection">The projection type.</typeparam>
    /// <typeparam name="TGrain">
    ///     The grain type decorated with
    ///     <see cref="Mississippi.EventSourcing.Brooks.Abstractions.Attributes.BrookNameAttribute" />.
    /// </typeparam>
    /// <param name="entityId">The entity identifier within the brook.</param>
    /// <param name="version">The specific version.</param>
    /// <returns>A versioned UX projection key for the specified projection, grain's brook, and version.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when <typeparamref name="TGrain" /> is not decorated with
    ///     <see cref="Mississippi.EventSourcing.Brooks.Abstractions.Attributes.BrookNameAttribute" />.
    /// </exception>
    public static UxProjectionVersionedKey ForGrain<TProjection, TGrain>(
        string entityId,
        BrookPosition version
    )
        where TGrain : class =>
        new(UxProjectionKey.ForGrain<TProjection, TGrain>(entityId), version);

    /// <summary>
    ///     Creates a versioned UX projection key from its string representation.
    /// </summary>
    /// <param name="value">The string value to convert.</param>
    /// <returns>A versioned UX projection key parsed from the string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="FormatException">Thrown when the string is not in the correct format.</exception>
    public static UxProjectionVersionedKey FromString(
        string value
    )
    {
        ArgumentNullException.ThrowIfNull(value);

        // Find the last separator which delimits the version
        int lastSeparator = value.LastIndexOf(Separator);
        if (lastSeparator < 0)
        {
            throw new FormatException(
                $"Versioned UX projection key must be in the form " +
                $"'<projectionTypeName>{Separator}<brookType>{Separator}<brookId>{Separator}<version>'.");
        }

        string projectionKeyPart = value[..lastSeparator];
        string versionPart = value[(lastSeparator + 1)..];
        if (!long.TryParse(versionPart, NumberStyles.None, CultureInfo.InvariantCulture, out long versionValue))
        {
            throw new FormatException($"Version '{versionPart}' is not a valid non-negative integer.");
        }

        UxProjectionKey projectionKey = UxProjectionKey.FromString(projectionKeyPart);
        return new(projectionKey, new(versionValue));
    }

    /// <summary>
    ///     Converts a versioned UX projection key to its string representation.
    /// </summary>
    /// <param name="key">The versioned UX projection key to convert.</param>
    /// <returns>A string representation in the format "projectionTypeName|brookType|brookId|version".</returns>
    public static string FromUxProjectionVersionedKey(
        UxProjectionVersionedKey key
    ) =>
        key;

    /// <summary>
    ///     Implicitly converts a <see cref="UxProjectionVersionedKey" /> to its string representation.
    /// </summary>
    /// <param name="key">The versioned UX projection key to convert.</param>
    /// <returns>A string representation in the format "projectionTypeName|brookType|brookId|version".</returns>
    public static implicit operator string(
        UxProjectionVersionedKey key
    ) =>
        $"{(string)key.ProjectionKey}{Separator}{key.Version.Value.ToString(CultureInfo.InvariantCulture)}";

    /// <summary>
    ///     Implicitly converts a string to a <see cref="UxProjectionVersionedKey" />.
    /// </summary>
    /// <param name="value">The string value to convert.</param>
    /// <returns>A versioned UX projection key parsed from the string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="FormatException">Thrown when the string is not in the correct format.</exception>
    public static implicit operator UxProjectionVersionedKey(
        string value
    ) =>
        FromString(value);
}