namespace Mississippi.Core.Streams.Grains;

public readonly record struct StreamCompositeKey
{
    private const char Separator = '|';

    private const int MaxLength = 1024;

    public StreamCompositeKey(
        string type,
        string id
    )
    {
        ValidateComponent(type, nameof(type));
        ValidateComponent(id, nameof(id));
        if ((type.Length + id.Length + 1) > MaxLength)
        {
            throw new ArgumentException($"Composite key exceeds the {MaxLength}-character limit.");
        }

        Type = type;
        Id = id;
    }

    public string Type { get; }

    public string Id { get; }

    public static implicit operator string(
        StreamCompositeKey key
    ) =>
        $"{key.Type}{Separator}{key.Id}";

    public static implicit operator StreamCompositeKey(
        string value
    )
    {
        ArgumentNullException.ThrowIfNull(value);
        int idx = value.IndexOf(Separator, StringComparison.Ordinal);
        if (idx < 0)
        {
            throw new FormatException($"Composite key must be in the form '<type>{Separator}<id>'.");
        }

        string type = value[..idx];
        string id = value[(idx + 1)..];
        return new(type, id);
    }

    public override string ToString() => this;

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
}