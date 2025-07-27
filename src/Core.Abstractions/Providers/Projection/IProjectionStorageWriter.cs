namespace Mississippi.Core.Abstractions.Providers.Projection;

public interface IProjectionStorageWriter
{
    Task WriteAsync<MississippiProjection>(string key, long version, CancellationToken cancellationToken = default);
}