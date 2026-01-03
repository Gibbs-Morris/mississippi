using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using Mississippi.EventSourcing.Brooks.Abstractions;

using Orleans;


namespace Mississippi.EventSourcing.UxProjections.Abstractions;

/// <summary>
///     Represents a composite key for identifying versioned UX projection caches,
///     consisting of a brook name, entity identifier, and version.
/// </summary>
/// <remarks>
///     <para>
///         Versioned UX projection cache keys extend the cursor key with a version
///         to enable caching of specific projection versions. This allows multiple clients
///         requesting the same version to share a cached grain instance.
///     </para>
///     <para>
///         The string format is "{BrookName}|{EntityId}|{Version}".
///     </para>
/// </remarks>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.UxProjections.Abstractions.UxProjectionVersionedCacheKey")]
public readonly record struct UxProjectionVersionedCacheKey
{
    private const int MaxLength = 2048;

    private const char Separator = '|';

    /// <summary>
    ///     Initializes a new instance of the <see cref="UxProjectionVersionedCacheKey" /> struct.
    /// </summary>
    /// <param name="brookName">The name of the brook (event stream type).</param>
    /// <param name="entityId">The identifier of the entity within the brook.</param>
    /// <param name="version">The specific version of the projection.</param>
    /// <exception cref="ArgumentNullException">Thrown when brookName or entityId is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when version is NotSet.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when the composite key exceeds the maximum length or contains invalid characters.
    /// </exception>
    public UxProjectionVersionedCacheKey(
        string brookName,
        string entityId,
        BrookPosition version
    )
    {
        ValidateBrookName(brookName);
        ValidateEntityId(entityId);
        if (version.NotSet)
        {
            throw new ArgumentOutOfRangeException(nameof(version), "Version cannot be NotSet (-1).");
        }

        string versionString = version.Value.ToString(CultureInfo.InvariantCulture);
        if ((brookName.Length + entityId.Length + versionString.Length + 2) > MaxLength)
        {
            throw new ArgumentException($"Composite key exceeds the {MaxLength}-character limit.");
        }

        BrookName = brookName;
        EntityId = entityId;
        Version = version;
    }

    /// <summary>
    ///     Gets the name of the brook (event stream type).
    /// </summary>
    [Id(0)]
    public string BrookName { get; }

    /// <summary>
    ///     Gets the identifier of the entity within the brook.
    /// </summary>
    [Id(1)]
    public string EntityId { get; }

    /// <summary>
    ///     Gets the specific version of the projection.
    /// </summary>
    [Id(2)]
    public BrookPosition Version { get; }

    /// <summary>
    ///     Creates a <see cref="UxProjectionVersionedCacheKey" /> from a <see cref="BrookKey" /> and version.
    /// </summary>
    /// <param name="brookKey">The brook key containing the brook name and entity ID.</param>
    /// <param name="version">The specific version of the projection.</param>
    /// <returns>A new <see cref="UxProjectionVersionedCacheKey" /> instance.</returns>
    public static UxProjectionVersionedCacheKey FromBrookKey(
        BrookKey brookKey,
        BrookPosition version
    ) =>
        new(brookKey.BrookName, brookKey.EntityId, version);

    /// <summary>
    ///     Creates a <see cref="UxProjectionVersionedCacheKey" /> from a <see cref="UxProjectionCursorKey" /> and version.
    /// </summary>
    /// <param name="cursorKey">The cursor key containing the brook name and entity ID.</param>
    /// <param name="version">The specific version of the projection.</param>
    /// <returns>A new <see cref="UxProjectionVersionedCacheKey" /> instance.</returns>
    public static UxProjectionVersionedCacheKey FromCursorKey(
        UxProjectionCursorKey cursorKey,
        BrookPosition version
    ) =>
        new(cursorKey.BrookName, cursorKey.EntityId, version);

    /// <summary>
    ///     Parses a string into a <see cref="UxProjectionVersionedCacheKey" />.
    /// </summary>
    /// <param name="keyString">The string to parse in the format "{BrookName}|{EntityId}|{Version}".</param>
    /// <returns>A new <see cref="UxProjectionVersionedCacheKey" /> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when keyString is null.</exception>
    /// <exception cref="FormatException">Thrown when the string format is invalid.</exception>
    public static UxProjectionVersionedCacheKey Parse(
        string keyString
    )
    {
        ArgumentNullException.ThrowIfNull(keyString);
        string[] parts = keyString.Split(Separator);
        if (parts.Length != 3)
        {
            throw new FormatException(
                $"Invalid UxProjectionVersionedCacheKey format. Expected 'brookName{Separator}entityId{Separator}version'.");
        }

        if (!long.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out long versionValue))
        {
            throw new FormatException("Version must be a valid integer.");
        }

        return new(parts[0], parts[1], new(versionValue));
    }

    /// <summary>
    ///     Tries to parse a string into a <see cref="UxProjectionVersionedCacheKey" />.
    /// </summary>
    /// <param name="keyString">The string to parse.</param>
    /// <param name="result">When successful, contains the parsed key.</param>
    /// <returns>True if parsing succeeded; otherwise, false.</returns>
    public static bool TryParse(
        string? keyString,
        out UxProjectionVersionedCacheKey result
    )
    {
        result = default;
        if (string.IsNullOrEmpty(keyString))
        {
            return false;
        }

        string[] parts = keyString.Split(Separator);
        if (parts.Length != 3)
        {
            return false;
        }

        if (!long.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out long versionValue))
        {
            return false;
        }

        try
        {
            result = new(parts[0], parts[1], new(versionValue));
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    ///     Converts a <see cref="UxProjectionVersionedCacheKey" /> to its string representation.
    /// </summary>
    /// <param name="key">The key to convert.</param>
    /// <returns>The string representation in the format "{BrookName}|{EntityId}|{Version}".</returns>
    [SuppressMessage(
        "Usage",
        "CA2225:Operator overloads have named alternates",
        Justification = "ToString() serves as the named alternate for this conversion.")]
    public static implicit operator string(
        UxProjectionVersionedCacheKey key
    ) =>
        $"{key.BrookName}{Separator}{key.EntityId}{Separator}{key.Version.Value}";

    private static void ValidateBrookName(
        string brookName
    )
    {
        ArgumentNullException.ThrowIfNull(brookName);
        if (string.IsNullOrWhiteSpace(brookName))
        {
            throw new ArgumentException("Brook name cannot be empty or whitespace.", nameof(brookName));
        }

        if (brookName.Contains(Separator, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Brook name cannot contain the separator character '{Separator}'.",
                nameof(brookName));
        }
    }

    private static void ValidateEntityId(
        string entityId
    )
    {
        ArgumentNullException.ThrowIfNull(entityId);
        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new ArgumentException("Entity ID cannot be empty or whitespace.", nameof(entityId));
        }

        if (entityId.Contains(Separator, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Entity ID cannot contain the separator character '{Separator}'.",
                nameof(entityId));
        }
    }

    /// <summary>
    ///     Converts this key to a <see cref="BrookKey" />.
    /// </summary>
    /// <returns>A <see cref="BrookKey" /> with the same brook name and entity ID.</returns>
    public BrookKey ToBrookKey() => new(BrookName, EntityId);

    /// <summary>
    ///     Converts this key to a <see cref="UxProjectionCursorKey" />.
    /// </summary>
    /// <returns>A <see cref="UxProjectionCursorKey" /> with the same brook name and entity ID.</returns>
    public UxProjectionCursorKey ToCursorKey() => new(BrookName, EntityId);

    /// <inheritdoc />
    public override string ToString() => this;
}