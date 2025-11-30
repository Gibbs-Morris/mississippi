using System;

using Orleans;


namespace Mississippi.EventSourcing.Abstractions;

/// <summary>
///     Represents a position within a brook.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.Abstractions.BrookPosition")]
public readonly record struct BrookPosition
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BrookPosition" /> struct with the specified value.
    /// </summary>
    /// <param name="value">The raw position value.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when <paramref name="value" /> is negative.
    /// </exception>
    public BrookPosition(
        long value
    )
    {
        if (value < -1)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Brook position cannot be less than -1.");
        }

        Value = value;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="BrookPosition" /> struct with the default value (-1).
    /// </summary>
    public BrookPosition() => Value = -1;

    /// <summary>
    ///     Gets a value indicating whether the position has not been set.
    /// </summary>
    /// <value>
    ///     <c>true</c> if <see cref="Value" /> is -1; otherwise, <c>false</c>.
    /// </value>
    public bool NotSet => Value == -1;

    /// <summary>
    ///     Gets the raw position value. A value of -1 indicates not set.
    /// </summary>
    [Id(0)]
    public long Value { get; }

    /// <summary>
    ///     Creates a <see cref="BrookPosition" /> from an <see cref="long" /> value.
    ///     Alternative method name for CA2225 compliance.
    /// </summary>
    /// <param name="value">The raw position value.</param>
    /// <returns>A new <see cref="BrookPosition" /> instance.</returns>
    public static BrookPosition FromInt64(
        long value
    ) =>
        new(value);

    /// <summary>
    ///     Creates a <see cref="BrookPosition" /> from a <see cref="long" /> value.
    /// </summary>
    /// <param name="value">The raw position value.</param>
    /// <returns>A new <see cref="BrookPosition" /> instance.</returns>
    public static BrookPosition FromLong(
        long value
    ) =>
        new(value);

    /// <summary>
    ///     Converts a <see cref="long" /> to a <see cref="BrookPosition" />.
    /// </summary>
    /// <param name="value">The raw position value.</param>
    /// <returns>The new <see cref="BrookPosition" /> instance.</returns>
    public static implicit operator BrookPosition(
        long value
    ) =>
        new(value);

    /// <summary>
    ///     Converts the <see cref="BrookPosition" /> to a <see cref="long" />.
    /// </summary>
    /// <param name="position">The <see cref="BrookPosition" /> to convert.</param>
    /// <returns>The raw <see cref="long" /> value.</returns>
    public static implicit operator long(
        BrookPosition position
    ) =>
        position.Value;

    /// <summary>
    ///     Determines whether this brook position is newer than another.
    /// </summary>
    /// <param name="other">The other <see cref="BrookPosition" /> to compare against.</param>
    /// <returns>
    ///     <c>true</c> if this position is greater than <paramref name="other" />; otherwise, <c>false</c>.
    /// </returns>
    public bool IsNewerThan(
        BrookPosition other
    ) =>
        Value > other.Value;

    /// <summary>
    ///     Converts this <see cref="BrookPosition" /> to an <see cref="long" />.
    ///     Alternative method name for CA2225 compliance.
    /// </summary>
    /// <returns>The raw <see cref="long" /> value.</returns>
    public long ToInt64() => Value;

    /// <summary>
    ///     Converts this <see cref="BrookPosition" /> to a <see cref="long" />.
    /// </summary>
    /// <returns>The raw <see cref="long" /> value.</returns>
    public long ToLong() => Value;
}