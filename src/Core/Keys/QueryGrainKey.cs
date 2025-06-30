namespace Mississippi.Core.Keys;

public sealed record QueryGrainKey : IOrleansKey
{
    public string Id { get; init; }

    public string ToOrleansKey() => Id;
}