using Mississippi.Core.Abstractions.Streams;

namespace Mississippi.Core.Abstractions.Providers.Storage;

public interface IBrookStorageWriter
{
    Task<BrookPosition> AppendEventsAsync(
        BrookKey brookId,
        IReadOnlyList<BrookEvent> events,
        BrookPosition? expectedVersion = null,
        CancellationToken cancellationToken = default
    );
}