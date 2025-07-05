using Mississippi.Core.Streams.Grains;
using Mississippi.Core.Streams.Grains.Reader;


namespace Mississippi.Core.Streams.StorageProvider;

public interface IStreamStorageWriter
{
    Task<StreamPosition> AppendEventsAsync(
        StreamCompositeKey streamId,
        IReadOnlyList<MississippiEvent> events,
        StreamPosition? expectedVersion = null,
        CancellationToken cancellationToken = default
    );
}