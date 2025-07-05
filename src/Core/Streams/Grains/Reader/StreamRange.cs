namespace Mississippi.Core.Streams.Grains.Reader;

public readonly record struct StreamRange(StreamPosition Start, int? Count = null);