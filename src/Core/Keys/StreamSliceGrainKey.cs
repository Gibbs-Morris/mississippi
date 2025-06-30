namespace Mississippi.Core.Keys;

public sealed record StreamSliceGrainKey : IOrleansKey
{
    // ie 12345-aaaa
    public string Id { get; init; }

    // ie user
    public string StreamType { get; init; }

    public long FromVersion { get; init; }

    public long ToVersion { get; init; }

    public string ToOrleansKey() => $"/name={StreamType}/id={Id}/f={FromVersion}/t={ToVersion}";
}