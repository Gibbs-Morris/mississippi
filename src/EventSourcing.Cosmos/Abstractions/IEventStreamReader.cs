using Mississippi.Core.Abstractions.Streams;

namespace Mississippi.EventSourcing.Cosmos.Abstractions;

internal interface IEventStreamReader
{
    IAsyncEnumerable<BrookEvent> ReadEventsAsync(BrookRangeKey brookRange, CancellationToken cancellationToken = default);
}