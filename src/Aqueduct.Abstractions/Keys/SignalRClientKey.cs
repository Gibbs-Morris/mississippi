using System;

using Orleans;


namespace Mississippi.Aqueduct.Abstractions.Keys;

/// <summary>
///     Represents a strongly-typed key for identifying SignalR client grains.
/// </summary>
/// <remarks>
///     <para>
///         The key format is "{HubName}:{ConnectionId}".
///     </para>
/// </remarks>
[GenerateSerializer]
[Alias("Mississippi.Aqueduct.SignalRClientKey")]
public readonly record struct SignalRClientKey
{
    private const int MaxLength = 4192;

    private const char Separator = ':';

    /// <summary>
    ///     Initializes a new instance of the <see cref="SignalRClientKey" /> struct.
    /// </summary>
    /// <param name="hubName">The name of the SignalR hub.</param>
    /// <param name="connectionId">The SignalR connection identifier.</param>
    /// <exception cref="ArgumentNullException">Thrown when hubName or connectionId is null.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when the composite key exceeds the maximum length or contains invalid
    ///     characters.
    /// </exception>
    public SignalRClientKey(
        string hubName,
        string connectionId
    )
    {
        ThrowIfInvalidComponent(hubName, nameof(hubName));
        ThrowIfInvalidComponent(connectionId, nameof(connectionId));
        if ((hubName.Length + connectionId.Length + 1) > MaxLength)
        {
            throw new ArgumentException($"Composite key exceeds the {MaxLength}-character limit.");
        }

        HubName = hubName;
        ConnectionId = connectionId;
    }

    /// <summary>
    ///     Gets the SignalR connection identifier.
    /// </summary>
    [Id(1)]
    public string ConnectionId { get => field ?? string.Empty; init; }

    /// <summary>
    ///     Gets the name of the SignalR hub.
    /// </summary>
    [Id(0)]
    public string HubName { get => field ?? string.Empty; init; }

    /// <summary>
    ///     Parses a string into a <see cref="SignalRClientKey" />.
    /// </summary>
    /// <param name="value">The string to parse in the format "{HubName}|{ConnectionId}".</param>
    /// <returns>A new <see cref="SignalRClientKey" /> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="FormatException">Thrown when the string format is invalid.</exception>
    public static SignalRClientKey Parse(
        string value
    )
    {
        ArgumentNullException.ThrowIfNull(value);
        int idx = value.IndexOf(Separator, StringComparison.Ordinal);
        if (idx < 0)
        {
            throw new FormatException($"SignalRClientKey must be in the form '<hubName>{Separator}<connectionId>'.");
        }

        string hubName = value[..idx];
        string connectionId = value[(idx + 1)..];
        return new(hubName, connectionId);
    }

    /// <summary>
    ///     Implicitly converts a <see cref="SignalRClientKey" /> to its string representation.
    /// </summary>
    /// <param name="key">The key to convert.</param>
    /// <returns>A string representation in the format "hubName:connectionId".</returns>
    public static implicit operator string(
        SignalRClientKey key
    ) =>
        $"{key.HubName}{Separator}{key.ConnectionId}";

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
    /// <returns>A string in the format "hubName:connectionId".</returns>
    public override string ToString() => this;
}