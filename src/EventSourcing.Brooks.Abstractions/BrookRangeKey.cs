using System;
using System.Globalization;

using Orleans;


namespace Mississippi.EventSourcing.Brooks.Abstractions;

/// <summary>
///     Represents a range key for querying brooks, consisting of brook name, entity id, start position, and count
///     components.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.Abstractions.BrookRangeKey")]
public readonly record struct BrookRangeKey
{
    private const int MaxLength = 1024;

    private const char Separator = '|';

    /// <summary>
    ///     Initializes a new instance of the <see cref="BrookRangeKey" /> struct.
    /// </summary>
    /// <param name="brookName">The brook name component of the key.</param>
    /// <param name="entityId">The entity id component of the key.</param>
    /// <param name="start">The starting position of the range.</param>
    /// <param name="count">The number of items in the range.</param>
    /// <exception cref="ArgumentNullException">Thrown when brookName or entityId is null.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when the composite key exceeds the maximum length or contains invalid
    ///     characters.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when start or count is negative.</exception>
    public BrookRangeKey(
        string brookName,
        string entityId,
        long start,
        long count
    )
    {
        ValidateComponent(brookName, nameof(brookName));
        ValidateComponent(entityId, nameof(entityId));
        ValidateRange(start, nameof(start));
        ValidateRange(count, nameof(count));

        // Rough length check (no allocation)
        // +3 separator chars
        if ((brookName.Length +
             entityId.Length +
             start.ToString(CultureInfo.InvariantCulture).Length +
             count.ToString(CultureInfo.InvariantCulture).Length +
             3) >
            MaxLength)
        {
            throw new ArgumentException($"Composite key exceeds the {MaxLength}-character limit.");
        }

        BrookName = brookName;
        EntityId = entityId;
        Start = start;
        Count = count;
    }

    /// <summary>
    ///     Gets the brook name component of the brook range key.
    /// </summary>
    [Id(0)]
    public string BrookName { get; }

    /// <summary>
    ///     Gets the count component of the brook range key.
    /// </summary>
    [Id(3)]
    public long Count { get; }

    /// <summary>
    ///     Gets the inclusive ending position of the range (Start + Count - 1).
    /// </summary>
    public BrookPosition End => (Start + Count) - 1;

    /// <summary>
    ///     Gets the entity id component of the brook range key.
    /// </summary>
    [Id(1)]
    public string EntityId { get; }

    /// <summary>
    ///     Gets the starting position of the range.
    /// </summary>
    [Id(2)]
    public BrookPosition Start { get; }

    /// <summary>
    ///     Creates a brook range key from a brook key and range parameters.
    /// </summary>
    /// <param name="key">The brook key containing brook name and entity id.</param>
    /// <param name="start">The starting position of the range.</param>
    /// <param name="count">The number of items in the range.</param>
    /// <returns>A new brook range key.</returns>
    public static BrookRangeKey FromBrookCompositeKey(
        BrookKey key,
        long start,
        long count
    ) =>
        new(key.BrookName, key.EntityId, start, count);

    /// <summary>
    ///     Converts a brook range key to its string representation.
    /// </summary>
    /// <param name="key">The brook range key to convert.</param>
    /// <returns>A string representation of the brook range key in the format "brookName|entityId|start|count".</returns>
    public static string FromBrookRangeKey(
        BrookRangeKey key
    ) =>
        key;

    /// <summary>
    ///     Creates a brook range key from its string representation.
    /// </summary>
    /// <param name="value">The string value to convert.</param>
    /// <returns>A brook range key parsed from the string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="FormatException">Thrown when the string is not in the correct format.</exception>
    public static BrookRangeKey FromString(
        string value
    )
    {
        ArgumentNullException.ThrowIfNull(value);

        // Fast path: split once; avoids allocations of String.Split
        Span<int> idx = stackalloc int[3];
        int found = 0;
        for (int i = 0; (i < value.Length) && (found < 3); i++)
        {
            if (value[i] == Separator)
            {
                idx[found++] = i;
            }
        }

        if (found != 3)
        {
            throw new FormatException(
                $"Composite key must be in the form '<brookName>{Separator}<entityId>{Separator}<start>{Separator}<count>'.");
        }

        string brookName = value[..idx[0]];
        string entityId = value[(idx[0] + 1)..idx[1]];
        ReadOnlySpan<char> startSpan = value.AsSpan(idx[1] + 1, idx[2] - idx[1] - 1);
        string countSpan = value[(idx[2] + 1)..];
        if (!long.TryParse(startSpan, out long start))
        {
            throw new FormatException($"Could not parse '{startSpan}' as a {nameof(Start)} (long).");
        }

        if (!long.TryParse(countSpan, out long count))
        {
            throw new FormatException($"Could not parse '{countSpan}' as a {nameof(Count)} (long).");
        }

        return new(brookName, entityId, start, count);
    }

    /// <summary>
    ///     Implicitly converts a <see cref="BrookRangeKey" /> to its string representation.
    /// </summary>
    /// <param name="key">The brook range key to convert.</param>
    /// <returns>A string representation of the brook range key in the format "brookName|entityId|start|count".</returns>
    public static implicit operator string(
        BrookRangeKey key
    ) =>
        $"{key.BrookName}{Separator}{key.EntityId}{Separator}{key.Start.Value}{Separator}{key.Count}";

    /// <summary>
    ///     Implicitly converts a string to a <see cref="BrookRangeKey" />.
    /// </summary>
    /// <param name="value">The string value to convert.</param>
    /// <returns>A brook range key parsed from the string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="FormatException">Thrown when the string is not in the correct format.</exception>
    public static implicit operator BrookRangeKey(
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

    private static void ValidateRange(
        long value,
        string paramName
    )
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, value, "Value must be non-negative.");
        }
    }

    /// <summary>
    ///     Converts this brook range key to a brook key containing only the brook name and entity id components.
    /// </summary>
    /// <returns>A brook key with the brook name and entity id from this range key.</returns>
    public BrookKey ToBrookCompositeKey() => new(BrookName, EntityId);

    /// <summary>
    ///     Returns the string representation of this brook range key.
    /// </summary>
    /// <returns>A string representation of the brook range key in the format "brookName|entityId|start|count".</returns>
    public override string ToString() => this;
}