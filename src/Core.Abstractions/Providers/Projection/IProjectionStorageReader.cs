namespace Mississippi.Core.Abstractions.Providers.Projection;

public interface IProjectionStorageReader
{
    Task<MississippiProjection> ReadAsync<MississippiProjection>(string key, long version,
        CancellationToken cancellationToken = default);
}