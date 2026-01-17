using System;

using Mississippi.EventSourcing.Brooks.Abstractions;

using Orleans;


namespace Mississippi.EventSourcing.UxProjections.Abstractions;

/// <summary>
///     Represents a key for identifying UX projection notification grains.
/// </summary>
/// <remarks>
///     <para>
///         Notification grains are keyed by projection type name, brook name, and entity ID
///         because they need to know which brook to subscribe to for cursor updates.
///     </para>
///     <para>
///         The string format is "{projectionTypeName}|{brookName}|{entityId}".
///     </para>
/// </remarks>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.UxProjections.Abstractions.UxProjectionNotificationKey")]
public readonly record struct UxProjectionNotificationKey
{
    private const int MaxLength = 4192;

    private const char Separator = '|';

    /// <summary>
    ///     Initializes a new instance of the <see cref="UxProjectionNotificationKey" /> struct.
    /// </summary>
    /// <param name="projectionTypeName">The name of the projection type.</param>
    /// <param name="brookKey">The brook key identifying the event stream.</param>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when any component contains the separator character or the composite key exceeds the maximum length.
    /// </exception>
    public UxProjectionNotificationKey(
        string projectionTypeName,
        BrookKey brookKey
    )
    {
        ArgumentNullException.ThrowIfNull(projectionTypeName);
        if (projectionTypeName.Contains(Separator, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Projection type name cannot contain the separator character '{Separator}'.",
                nameof(projectionTypeName));
        }

        // Validate brookKey components don't contain separator
        if (brookKey.BrookName.Contains(Separator, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Brook name cannot contain the separator character '{Separator}'.",
                nameof(brookKey));
        }

        if (brookKey.EntityId.Contains(Separator, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Entity ID cannot contain the separator character '{Separator}'.",
                nameof(brookKey));
        }

        // 2 separators in "projectionTypeName|brookName|entityId"
        int totalLength = projectionTypeName.Length + brookKey.BrookName.Length + brookKey.EntityId.Length + 2;
        if (totalLength > MaxLength)
        {
            throw new ArgumentException($"Composite key exceeds the {MaxLength}-character limit.");
        }

        ProjectionTypeName = projectionTypeName;
        BrookKey = brookKey;
    }

    /// <summary>
    ///     Gets the brook key identifying the event stream.
    /// </summary>
    [Id(1)]
    public BrookKey BrookKey { get; }

    /// <summary>
    ///     Gets the entity identifier (convenience accessor for BrookKey.EntityId).
    /// </summary>
    public string EntityId => BrookKey.EntityId;

    /// <summary>
    ///     Gets the name of the projection type.
    /// </summary>
    [Id(0)]
    public string ProjectionTypeName { get; }

    /// <summary>
    ///     Creates a notification key from its string representation.
    /// </summary>
    /// <param name="value">The string value to convert.</param>
    /// <returns>A notification key parsed from the string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="FormatException">Thrown when the string is not in the correct format.</exception>
    public static UxProjectionNotificationKey FromString(
        string value
    )
    {
        ArgumentNullException.ThrowIfNull(value);
        int firstSeparator = value.IndexOf(Separator, StringComparison.Ordinal);
        if (firstSeparator < 0)
        {
            throw new FormatException(
                $"Notification key must be in the form '<projectionTypeName>{Separator}<brookName>{Separator}<entityId>'.");
        }

        int secondSeparator = value.IndexOf(Separator, firstSeparator + 1);
        if (secondSeparator < 0)
        {
            throw new FormatException(
                $"Notification key must be in the form '<projectionTypeName>{Separator}<brookName>{Separator}<entityId>'.");
        }

        string projectionTypeName = value[..firstSeparator];
        string brookName = value[(firstSeparator + 1)..secondSeparator];
        string entityId = value[(secondSeparator + 1)..];
        return new(projectionTypeName, new(brookName, entityId));
    }

    /// <summary>
    ///     Converts a notification key to its string representation.
    /// </summary>
    /// <param name="key">The notification key to convert.</param>
    /// <returns>A string representation in the format "projectionTypeName|brookName|entityId".</returns>
    public static string FromUxProjectionNotificationKey(
        UxProjectionNotificationKey key
    ) =>
        key;

    /// <summary>
    ///     Implicitly converts a <see cref="UxProjectionNotificationKey" /> to its string representation.
    /// </summary>
    /// <param name="key">The notification key to convert.</param>
    /// <returns>A string representation in the format "projectionTypeName|brookName|entityId".</returns>
    public static implicit operator string(
        UxProjectionNotificationKey key
    ) =>
        $"{key.ProjectionTypeName}{Separator}{key.BrookKey.BrookName}{Separator}{key.BrookKey.EntityId}";

    /// <summary>
    ///     Implicitly converts a string to a <see cref="UxProjectionNotificationKey" />.
    /// </summary>
    /// <param name="value">The string value to convert.</param>
    /// <returns>A notification key parsed from the string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="FormatException">Thrown when the string is not in the correct format.</exception>
    public static implicit operator UxProjectionNotificationKey(
        string value
    ) =>
        FromString(value);

    /// <summary>
    ///     Returns the string representation of this key.
    /// </summary>
    /// <returns>The key in "projectionTypeName|brookName|entityId" format.</returns>
    public override string ToString() => this;
}