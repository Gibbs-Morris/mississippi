using Mississippi.Core.Abstractions.Streams;

namespace Mississippi.EventSourcing.Cosmos.Abstractions;

internal interface IEventStreamAppender
{
    Task<BrookPosition> AppendEventsAsync(BrookKey brookId, IReadOnlyList<BrookEvent> events, BrookPosition? expectedVersion, CancellationToken cancellationToken = default);
}