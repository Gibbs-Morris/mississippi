namespace Mississippi.Core.Keys;

public sealed class MississippiGrainKeyFactory
{
    private IOrleansKey CreateQueryGrainKey(
        string id
    ) =>
        new QueryGrainKey
        {
            Id = id,
        };

    private IOrleansKey CreateVersionedQueryGrainKey(
        string id,
        long version
    ) =>
        new VersionedQueryGrainKey
        {
            Id = id,
            Version = version,
        };
}