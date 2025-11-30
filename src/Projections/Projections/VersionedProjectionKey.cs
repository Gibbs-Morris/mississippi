using System;

using Orleans;


namespace Mississippi.Projections.Projections;

/// <summary>
///     Represents a versioned composite key for identifying projection snapshots and builders.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.Projections.Projections.VersionedProjectionKey")]
public readonly record struct VersionedProjectionKey
{
    private const int MaxLength = 1024;

    private const char Separator = '|';

    /// <summary>
    ///     Initializes a new instance of the <see cref="VersionedProjectionKey" /> struct.
    /// </summary>
    /// <param name="path">The projection path component of the key.</param>
    /// <param name="id">The identifier component of the key.</param>
    /// <param name="version">The version component of the key.</param>
    /// <exception cref="ArgumentNullException">Thrown when path or id is null.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when the composite key exceeds the maximum length or contains invalid
    ///     characters.
    /// </exception>
    public VersionedProjectionKey(
        string path,
        string id,
        long version
    )
    {
        ValidateComponent(path, nameof(path));
        ValidateComponent(id, nameof(id));
        if ((path.Length + id.Length + 1) > MaxLength)
        {
            throw new ArgumentException($"Composite key exceeds the {MaxLength}-character limit.");
        }

        Path = path;
        Id = id;
        Version = version;
    }

    /// <summary>
    ///     Gets the identifier component of the versioned projection key.
    /// </summary>
    [Id(1)]
    public string Id { get; }

    /// <summary>
    ///     Gets the projection path component of the key.
    /// </summary>
    [Id(0)]
    public string Path { get; }

    /// <summary>
    ///     Gets the version component of the key.
    /// </summary>
    [Id(2)]
    public long Version { get; }

    /// <summary>
    ///     Creates a versioned projection key from its string representation.
    /// </summary>
    /// <param name="value">The string value to convert.</param>
    /// <returns>A versioned projection key parsed from the string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="FormatException">Thrown when the string is not in the correct format.</exception>
    public static VersionedProjectionKey FromString(
        string value
    )
    {
        ArgumentNullException.ThrowIfNull(value);
        string[] parts = value.Split(Separator);
        if (parts.Length != 3)
        {
            throw new FormatException(
                $"Composite key must be in the form '<path>{Separator}<id>{Separator}<version>'.");
        }

        string path = parts[0];
        string id = parts[1];
        if (!long.TryParse(parts[2], out long version))
        {
            throw new FormatException("Version component must be a valid long value.");
        }

        return new(path, id, version);
    }

    /// <summary>
    ///     Converts a versioned projection key to its string representation.
    /// </summary>
    /// <param name="key">The versioned projection key to convert.</param>
    /// <returns>A string representation of the versioned projection key in the format "path|id|version".</returns>
    public static string FromVersionedProjectionKey(
        VersionedProjectionKey key
    ) =>
        key;

    /// <summary>
    ///     Implicitly converts a <see cref="VersionedProjectionKey" /> to its string representation.
    /// </summary>
    /// <param name="key">The versioned projection key to convert.</param>
    /// <returns>A string representation of the versioned projection key in the format "path|id|version".</returns>
    public static implicit operator string(
        VersionedProjectionKey key
    ) =>
        $"{key.Path}{Separator}{key.Id}{Separator}{key.Version}";

    /// <summary>
    ///     Implicitly converts a string to a <see cref="VersionedProjectionKey" />.
    /// </summary>
    /// <param name="value">The string value to convert.</param>
    /// <returns>A versioned projection key parsed from the string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="FormatException">Thrown when the string is not in the correct format.</exception>
    public static implicit operator VersionedProjectionKey(
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
    ///     Returns the string representation of this versioned projection key.
    /// </summary>
    /// <returns>A string representation of the versioned projection key in the format "path|id|version".</returns>
    public override string ToString() => this;
}