namespace Mississippi.Core.Abstractions.Brooks;

/// <summary>
///     Represents a composite key for identifying brooks, consisting of a type and id component.
/// </summary>
public readonly record struct BrookKey
{
    private const char Separator = '|';

    private const int MaxLength = 1024;

    /// <summary>
    ///     Initializes a new instance of the <see cref="BrookKey" /> struct.
    /// </summary>
    /// <param name="type">The type component of the key.</param>
    /// <param name="id">The id component of the key.</param>
    /// <exception cref="ArgumentNullException">Thrown when type or id is null.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when the composite key exceeds the maximum length or contains invalid
    ///     characters.
    /// </exception>
    public BrookKey(
        string type,
        string id
    )
    {
        ValidateComponent(type, nameof(type));
        ValidateComponent(id, nameof(id));
        if ((type.Length + id.Length + 1) > MaxLength)
        {
            throw new ArgumentException($"Composite key exceeds the {MaxLength}-character limit.");
        }

        Type = type;
        Id = id;
    }

    /// <summary>
    ///     Gets the type component of the brook key.
    /// </summary>
    public string Type { get; }

    /// <summary>
    ///     Gets the id component of the brook key.
    /// </summary>
    public string Id { get; }

    /// <summary>
    ///     Implicitly converts a <see cref="BrookKey" /> to its string representation.
    /// </summary>
    /// <param name="key">The brook key to convert.</param>
    /// <returns>A string representation of the brook key in the format "type|id".</returns>
    public static implicit operator string(
        BrookKey key
    ) =>
        $"{key.Type}{Separator}{key.Id}";

    /// <summary>
    ///     Converts a brook key to its string representation.
    /// </summary>
    /// <param name="key">The brook key to convert.</param>
    /// <returns>A string representation of the brook key in the format "type|id".</returns>
    public static string FromBrookKey(
        BrookKey key
    ) =>
        key;

    /// <summary>
    ///     Implicitly converts a string to a <see cref="BrookKey" />.
    /// </summary>
    /// <param name="value">The string value to convert.</param>
    /// <returns>A brook key parsed from the string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="FormatException">Thrown when the string is not in the correct format.</exception>
    public static implicit operator BrookKey(
        string value
    ) =>
        FromString(value);

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
            throw new FormatException($"Composite key must be in the form '<type>{Separator}<id>'.");
        }

        string type = value[..idx];
        string id = value[(idx + 1)..];
        return new(type, id);
    }

    /// <summary>
    ///     Returns the string representation of this brook key.
    /// </summary>
    /// <returns>A string representation of the brook key in the format "type|id".</returns>
    public override string ToString() => this;

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
}