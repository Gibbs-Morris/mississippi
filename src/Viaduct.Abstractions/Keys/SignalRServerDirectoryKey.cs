using System;

using Orleans;


namespace Mississippi.Viaduct;

/// <summary>
///     Represents a strongly-typed key for identifying SignalR server directory grains.
/// </summary>
/// <remarks>
///     <para>
///         Server directory grains are typically singletons with a constant key like "default".
///     </para>
/// </remarks>
[GenerateSerializer]
[Alias("Mississippi.Viaduct.SignalRServerDirectoryKey")]
public readonly record struct SignalRServerDirectoryKey
{
    private const int MaxLength = 4192;

    /// <summary>
    ///     The default key for the singleton server directory grain.
    /// </summary>
    public static readonly SignalRServerDirectoryKey Default = new("default");

    /// <summary>
    ///     Initializes a new instance of the <see cref="SignalRServerDirectoryKey" /> struct.
    /// </summary>
    /// <param name="value">The key value (typically "default").</param>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the key exceeds the maximum length.</exception>
    public SignalRServerDirectoryKey(
        string value
    )
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length > MaxLength)
        {
            throw new ArgumentException($"Key exceeds the {MaxLength}-character limit.", nameof(value));
        }

        Value = value;
    }

    /// <summary>
    ///     Gets the key value.
    /// </summary>
    [Id(0)]
    public string Value { get; }

    /// <summary>
    ///     Parses a string into a <see cref="SignalRServerDirectoryKey" />.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <returns>A new <see cref="SignalRServerDirectoryKey" /> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    public static SignalRServerDirectoryKey Parse(
        string value
    )
    {
        ArgumentNullException.ThrowIfNull(value);
        return new(value);
    }

    /// <summary>
    ///     Implicitly converts a <see cref="SignalRServerDirectoryKey" /> to its string representation.
    /// </summary>
    /// <param name="key">The key to convert.</param>
    /// <returns>The key value string.</returns>
    public static implicit operator string(
        SignalRServerDirectoryKey key
    ) =>
        key.Value;

    /// <summary>
    ///     Returns the string representation of this key.
    /// </summary>
    /// <returns>The key value.</returns>
    public override string ToString() => Value;
}