namespace Mississippi.Core.Abstractions.Streams;

public readonly record struct BrookKey
{
    private const char Separator = '|';

    private const int MaxLength = 1024;

    public BrookKey(
        string type,
        string id
    )
    {
        ValidateComponent(type, nameof(type));
        ValidateComponent(id, nameof(id));
        if (type.Length + id.Length + 1 > MaxLength)
            throw new ArgumentException($"Composite key exceeds the {MaxLength}-character limit.");

        Type = type;
        Id = id;
    }

    public string Type { get; }

    public string Id { get; }

    public static implicit operator string(
        BrookKey key
    )
    {
        return $"{key.Type}{Separator}{key.Id}";
    }

    public static implicit operator BrookKey(
        string value
    )
    {
        ArgumentNullException.ThrowIfNull(value);
        var idx = value.IndexOf(Separator, StringComparison.Ordinal);
        if (idx < 0) throw new FormatException($"Composite key must be in the form '<type>{Separator}<id>'.");

        var type = value[..idx];
        var id = value[(idx + 1)..];
        return new BrookKey(type, id);
    }

    public override string ToString()
    {
        return this;
    }

    private static void ValidateComponent(
        string value,
        string paramName
    )
    {
        if (value is null) throw new ArgumentNullException(paramName);

        if (value.Contains(Separator, StringComparison.Ordinal))
            throw new ArgumentException($"Value cannot contain the separator character '{Separator}'.", paramName);
    }
}