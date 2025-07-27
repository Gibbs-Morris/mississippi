using Mississippi.Core.Abstractions.Streams;

namespace Mississippi.Core.Abstractions.Providers.Storage;

public interface IBrookStorageReader
{
    Task<BrookPosition> ReadHeadPositionAsync(
        BrookKey brookId,
        CancellationToken cancellationToken = default
    );

    IAsyncEnumerable<BrookEvent> ReadEventsAsync(
        BrookRangeKey brookRange,
        CancellationToken cancellationToken = default
    );
}