using System;

using Orleans;


namespace Mississippi.Projections.Projections;

/// <summary>
///     Represents a composite key for identifying projections, consisting of a path and id component.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.Projections.Projections.ProjectionKey")]
public readonly record struct ProjectionKey
{
    private const int MaxLength = 1024;

    private const char Separator = '|';

    /// <summary>
    ///     Initializes a new instance of the <see cref="ProjectionKey" /> struct.
    /// </summary>
    /// <param name="path">The projection path component of the key.</param>
    /// <param name="id">The identifier component of the key.</param>
    /// <exception cref="ArgumentNullException">Thrown when path or id is null.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when the composite key exceeds the maximum length or contains invalid
    ///     characters.
    /// </exception>
    public ProjectionKey(
        string path,
        string id
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
    }

    /// <summary>
    ///     Gets the identifier component of the projection key.
    /// </summary>
    [Id(1)]
    public string Id { get; }

    /// <summary>
    ///     Gets the projection path component of the key.
    /// </summary>
    [Id(0)]
    public string Path { get; }

    /// <summary>
    ///     Converts a projection key to its string representation.
    /// </summary>
    /// <param name="key">The projection key to convert.</param>
    /// <returns>A string representation of the projection key in the format "path|id".</returns>
    public static string FromProjectionKey(
        ProjectionKey key
    ) =>
        key;

    /// <summary>
    ///     Creates a projection key from its string representation.
    /// </summary>
    /// <param name="value">The string value to convert.</param>
    /// <returns>A projection key parsed from the string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="FormatException">Thrown when the string is not in the correct format.</exception>
    public static ProjectionKey FromString(
        string value
    )
    {
        ArgumentNullException.ThrowIfNull(value);
        int idx = value.IndexOf(Separator, StringComparison.Ordinal);
        if (idx < 0)
        {
            throw new FormatException($"Composite key must be in the form '<path>{Separator}<id>'.");
        }

        string path = value[..idx];
        string id = value[(idx + 1)..];
        return new(path, id);
    }

    /// <summary>
    ///     Implicitly converts a <see cref="ProjectionKey" /> to its string representation.
    /// </summary>
    /// <param name="key">The projection key to convert.</param>
    /// <returns>A string representation of the projection key in the format "path|id".</returns>
    public static implicit operator string(
        ProjectionKey key
    ) =>
        $"{key.Path}{Separator}{key.Id}";

    /// <summary>
    ///     Implicitly converts a string to a <see cref="ProjectionKey" />.
    /// </summary>
    /// <param name="value">The string value to convert.</param>
    /// <returns>A projection key parsed from the string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="FormatException">Thrown when the string is not in the correct format.</exception>
    public static implicit operator ProjectionKey(
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
    ///     Returns the string representation of this projection key.
    /// </summary>
    /// <returns>A string representation of the projection key in the format "path|id".</returns>
    public override string ToString() => this;
}