using Mississippi.Core.Streams.Grains;
using Mississippi.Core.Streams.Grains.Reader;


namespace Mississippi.Core.Streams.StorageProvider;

public interface IStreamStorageReader
{
    Task<long> ReadHeadPositionAsync(
        StreamCompositeKey streamId,
        CancellationToken cancellationToken = default
    );

    IAsyncEnumerable<MississippiEvent> ReadEventsAsync(
        StreamCompositeRangeKey streamRange,
        CancellationToken cancellationToken = default
    );
}