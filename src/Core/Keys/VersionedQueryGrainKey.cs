namespace Mississippi.Core.Keys;

public sealed record VersionedQueryGrainKey : IOrleansKey
{
    public string Id { get; init; }

    public long Version { get; init; }

    public string ToOrleansKey() => $"/id={Id}/v+{Version}";
}