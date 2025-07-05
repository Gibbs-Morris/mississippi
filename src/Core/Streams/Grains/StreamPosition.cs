namespace Mississippi.Core.Streams.Grains;

public readonly record struct StreamPosition(long Value)
{
    // long  ➜ StreamPosition
    public static implicit operator StreamPosition(
        long value
    ) =>
        new(value);

    // StreamPosition ➜ long
    public static implicit operator long(
        StreamPosition position
    ) =>
        position.Value;
}