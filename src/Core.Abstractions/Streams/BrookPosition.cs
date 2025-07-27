namespace Mississippi.Core.Abstractions.Streams;



/// <summary>
///     Represents a position within a stream.
/// </summary>
public readonly record struct BrookPosition
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BrookPosition" /> struct with the specified value.
    /// </summary>
    /// <param name="value">The raw position value.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when <paramref name="value" /> is negative.
    /// </exception>
    public BrookPosition(long value)
    {
        if (value < -1)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Stream position cannot be less than -1.");
        }

        Value = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BrookPosition"/> struct with the default value (-1).
    /// </summary>
    public BrookPosition()
    {
        Value = -1;
    }


    /// <summary>
    ///     Gets the raw position value. A value of -1 indicates not set.
    /// </summary>
    public long Value { get; }

    /// <summary>
    ///     Gets a value indicating whether the position has not been set.
    /// </summary>
    /// <value>
    ///     <c>true</c> if <see cref="Value"/> is -1; otherwise, <c>false</c>.
    /// </value>
    public bool NotSet => Value == -1;

    /// <summary>
    ///     Converts a <see cref="long" /> to a <see cref="BrookPosition" />.
    /// </summary>
    /// <param name="value">The raw position value.</param>
    /// <returns>The new <see cref="BrookPosition" /> instance.</returns>
    public static implicit operator BrookPosition(long value)
    {
        return new BrookPosition(value);
    }

    /// <summary>
    ///     Converts the <see cref="BrookPosition" /> to a <see cref="long" />.
    /// </summary>
    /// <param name="position">The <see cref="BrookPosition" /> to convert.</param>
    /// <returns>The raw <see cref="long" /> value.</returns>
    public static implicit operator long(BrookPosition position)
    {
        return position.Value;
    }

    /// <summary>
    ///     Creates a <see cref="BrookPosition"/> from a <see cref="long"/> value.
    /// </summary>
    /// <param name="value">The raw position value.</param>
    /// <returns>A new <see cref="BrookPosition"/> instance.</returns>
    public static BrookPosition FromLong(long value) => new BrookPosition(value);

    /// <summary>
    ///     Converts this <see cref="BrookPosition"/> to a <see cref="long"/>.
    /// </summary>
    /// <returns>The raw <see cref="long"/> value.</returns>
    public long ToLong() => Value;

    /// <summary>
    ///     Determines whether this stream position is newer than another.
    /// </summary>
    /// <param name="other">The other <see cref="BrookPosition" /> to compare against.</param>
    /// <returns>
    ///     <c>true</c> if this position is greater than <paramref name="other" />; otherwise, <c>false</c>.
    /// </returns>
    public bool IsNewerThan(BrookPosition other)
    {
        return Value > other.Value;
    }
}