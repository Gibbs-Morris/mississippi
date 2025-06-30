namespace Mississippi.Core.Keys;

public sealed class MississippiGrainKeyFactory
{
    private IOrleansKey CreateQueryGrainKey(
        string id
    )
    {
        return new QueryGrainKey
        {
            Id = id,
        };
    }

    private IOrleansKey CreateVersionedQueryGrainKey(
        string id,
        long version
    )
    {
        return new VersionedQueryGrainKey
        {
            Id = id,
            Version = version,
        };
    }
}