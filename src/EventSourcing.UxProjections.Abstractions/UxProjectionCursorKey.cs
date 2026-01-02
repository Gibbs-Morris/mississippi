using System;
using System.Diagnostics.CodeAnalysis;

using Mississippi.EventSourcing.Brooks.Abstractions;

using Orleans;


namespace Mississippi.EventSourcing.UxProjections.Abstractions;

/// <summary>
///     Represents a composite key for identifying UX projection cursors,
///     consisting of a brook name and entity identifier.
/// </summary>
/// <remarks>
///     <para>
///         UX projection cursors track the last consumed position for a specific entity
///         within a brook. The cursor only needs the brook name and entity ID to identify
///         its position in the event stream.
///     </para>
///     <para>
///         The string format is "{BrookName}|{EntityId}".
///     </para>
/// </remarks>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.UxProjections.Abstractions.UxProjectionCursorKey")]
public readonly record struct UxProjectionCursorKey
{
    private const int MaxLength = 2048;

    private const char Separator = '|';

    /// <summary>
    ///     Initializes a new instance of the <see cref="UxProjectionCursorKey" /> struct.
    /// </summary>
    /// <param name="brookName">The name of the brook (event stream type).</param>
    /// <param name="entityId">The identifier of the entity within the brook.</param>
    /// <exception cref="ArgumentNullException">Thrown when brookName or entityId is null.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when the composite key exceeds the maximum length or contains invalid characters.
    /// </exception>
    public UxProjectionCursorKey(
        string brookName,
        string entityId
    )
    {
        ValidateBrookName(brookName);
        ValidateEntityId(entityId);
        if ((brookName.Length + entityId.Length + 1) > MaxLength)
        {
            throw new ArgumentException($"Composite key exceeds the {MaxLength}-character limit.");
        }

        BrookName = brookName;
        EntityId = entityId;
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
    ///     Creates a <see cref="UxProjectionCursorKey" /> from a <see cref="BrookKey" />.
    /// </summary>
    /// <param name="brookKey">The brook key containing the brook name and entity ID.</param>
    /// <returns>A new <see cref="UxProjectionCursorKey" /> instance.</returns>
    public static UxProjectionCursorKey FromBrookKey(
        BrookKey brookKey
    ) =>
        new(brookKey.BrookName, brookKey.EntityId);

    /// <summary>
    ///     Parses a string into a <see cref="UxProjectionCursorKey" />.
    /// </summary>
    /// <param name="keyString">The string to parse in the format "{BrookName}|{EntityId}".</param>
    /// <returns>A new <see cref="UxProjectionCursorKey" /> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when keyString is null.</exception>
    /// <exception cref="FormatException">Thrown when the string format is invalid.</exception>
    public static UxProjectionCursorKey Parse(
        string keyString
    )
    {
        ArgumentNullException.ThrowIfNull(keyString);
        string[] parts = keyString.Split(Separator);
        if (parts.Length != 2)
        {
            throw new FormatException(
                $"Invalid UxProjectionCursorKey format. Expected 'brookName{Separator}entityId'.");
        }

        return new(parts[0], parts[1]);
    }

    /// <summary>
    ///     Tries to parse a string into a <see cref="UxProjectionCursorKey" />.
    /// </summary>
    /// <param name="keyString">The string to parse.</param>
    /// <param name="result">When successful, contains the parsed key.</param>
    /// <returns>True if parsing succeeded; otherwise, false.</returns>
    public static bool TryParse(
        string? keyString,
        out UxProjectionCursorKey result
    )
    {
        result = default;
        if (string.IsNullOrEmpty(keyString))
        {
            return false;
        }

        string[] parts = keyString.Split(Separator);
        if (parts.Length != 2)
        {
            return false;
        }

        try
        {
            result = new(parts[0], parts[1]);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    ///     Converts a <see cref="UxProjectionCursorKey" /> to its string representation.
    /// </summary>
    /// <param name="key">The key to convert.</param>
    /// <returns>The string representation in the format "{BrookName}|{EntityId}".</returns>
    [SuppressMessage(
        "Usage",
        "CA2225:Operator overloads have named alternates",
        Justification = "ToString() serves as the named alternate for this conversion.")]
    public static implicit operator string(
        UxProjectionCursorKey key
    ) =>
        $"{key.BrookName}{Separator}{key.EntityId}";

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

    /// <inheritdoc />
    public override string ToString() => this;
}