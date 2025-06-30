namespace Mississippi.Core.Keys;

public sealed record StreamGrainKey : IOrleansKey
{
    // ie 12345-aaaa
    public string Id { get; init; }

    // ie user
    public string StreamType { get; init; }

    public string ToOrleansKey()
    {
        return $"/name={StreamType}/id={Id}";
    }
}