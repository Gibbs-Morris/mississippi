using System;

using Orleans;


namespace Mississippi.Aqueduct.Abstractions.Keys;

/// <summary>
///     Represents a strongly-typed key for identifying SignalR group grains.
/// </summary>
/// <remarks>
///     <para>
///         The key format is "{HubName}:{GroupName}".
///     </para>
/// </remarks>
[GenerateSerializer]
[Alias("Mississippi.Aqueduct.SignalRGroupKey")]
public readonly record struct SignalRGroupKey
{
    private const int MaxLength = 4192;

    private const char Separator = ':';

    /// <summary>
    ///     Initializes a new instance of the <see cref="SignalRGroupKey" /> struct.
    /// </summary>
    /// <param name="hubName">The name of the SignalR hub.</param>
    /// <param name="groupName">The name of the SignalR group.</param>
    /// <exception cref="ArgumentNullException">Thrown when hubName or groupName is null.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when the composite key exceeds the maximum length or contains invalid
    ///     characters.
    /// </exception>
    public SignalRGroupKey(
        string hubName,
        string groupName
    )
    {
        ThrowIfInvalidComponent(hubName, nameof(hubName));
        ThrowIfInvalidComponent(groupName, nameof(groupName));
        if ((hubName.Length + groupName.Length + 1) > MaxLength)
        {
            throw new ArgumentException($"Composite key exceeds the {MaxLength}-character limit.");
        }

        HubName = hubName;
        GroupName = groupName;
    }

    /// <summary>
    ///     Gets the name of the SignalR group.
    /// </summary>
    [Id(1)]
    public string GroupName { get => field ?? string.Empty; init; }

    /// <summary>
    ///     Gets the name of the SignalR hub.
    /// </summary>
    [Id(0)]
    public string HubName { get => field ?? string.Empty; init; }

    /// <summary>
    ///     Parses a string into a <see cref="SignalRGroupKey" />.
    /// </summary>
    /// <param name="value">The string to parse in the format "{HubName}|{GroupName}".</param>
    /// <returns>A new <see cref="SignalRGroupKey" /> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="FormatException">Thrown when the string format is invalid.</exception>
    public static SignalRGroupKey Parse(
        string value
    )
    {
        ArgumentNullException.ThrowIfNull(value);
        int idx = value.IndexOf(Separator, StringComparison.Ordinal);
        if (idx < 0)
        {
            throw new FormatException($"SignalRGroupKey must be in the form '<hubName>{Separator}<groupName>'.");
        }

        string hubName = value[..idx];
        string groupName = value[(idx + 1)..];
        return new(hubName, groupName);
    }

    /// <summary>
    ///     Implicitly converts a <see cref="SignalRGroupKey" /> to its string representation.
    /// </summary>
    /// <param name="key">The key to convert.</param>
    /// <returns>A string representation in the format "hubName:groupName".</returns>
    public static implicit operator string(
        SignalRGroupKey key
    ) =>
        $"{key.HubName}{Separator}{key.GroupName}";

    private static void ThrowIfInvalidComponent(
        string? value,
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
    ///     Returns the string representation of this key.
    /// </summary>
    /// <returns>A string in the format "hubName:groupName".</returns>
    public override string ToString() => this;
}