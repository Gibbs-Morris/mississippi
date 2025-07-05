namespace Mississippi.Core.Streams.Grains;

public readonly record struct StreamCompositeRangeKey
{
    private const char Separator = '|';

    private const int MaxLength = 1024;

    public StreamCompositeRangeKey(
        string type,
        string id,
        long start,
        long count
    )
    {
        ValidateComponent(type, nameof(type));
        ValidateComponent(id, nameof(id));
        ValidateRange(start, nameof(start));
        ValidateRange(count, nameof(count));

        // Rough length check (no allocation)
        // +3 separator chars
        if ((type.Length + id.Length + start.ToString().Length + count.ToString().Length + 3) > MaxLength)
        {
            throw new ArgumentException($"Composite key exceeds the {MaxLength}-character limit.");
        }

        Type = type;
        Id = id;
        Start = start;
        Count = count;
    }

    public string Type { get; }

    public string Id { get; }

    public long Start { get; }

    public long Count { get; }

    public static StreamCompositeRangeKey FromStreamCompositeKey(
        StreamCompositeKey key,
        long start,
        long count
    ) =>
        new(key.Type, key.Id, start, count);

    public override string ToString() => this;

    #region Implicit conversions

    public static implicit operator string(
        StreamCompositeRangeKey key
    ) =>
        $"{key.Type}{Separator}{key.Id}{Separator}{key.Start}{Separator}{key.Count}";

    public static implicit operator StreamCompositeRangeKey(
        string value
    )
    {
        ArgumentNullException.ThrowIfNull(value);

        // Fast path: split once; avoids allocations of String.Split
        Span<int> idx = stackalloc int[3];
        int found = 0, pos = -1;
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
                $"Composite key must be in the form '<type>{Separator}<id>{Separator}<start>{Separator}<count>'.");
        }

        string type = value[..idx[0]];
        string id = value[(idx[0] + 1)..idx[1]];
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

        return new(type, id, start, count);
    }

    #endregion

    #region Validation helpers

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

    #endregion
}