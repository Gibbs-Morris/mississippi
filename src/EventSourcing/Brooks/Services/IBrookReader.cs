using System.Collections.Immutable;

using Mississippi.EventSourcing.Abstractions.Brooks;


namespace Mississippi.EventSourcing.Brooks.Services;

// Public entry point.
public interface IBrookReader
{
    IAsyncEnumerable<BrookEvent> ReadEventsAsync(
        BrookKey brookKey,
        BrookPosition? readFrom = null,
        BrookPosition? readTo = null,
        CancellationToken cancellationToken = default
    );

    Task<ImmutableArray<BrookEvent>> ReadEventsBatchAsync(
        BrookKey brookKey,
        BrookPosition? readFrom = null,
        BrookPosition? readTo = null,
        CancellationToken cancellationToken = default
    );
}