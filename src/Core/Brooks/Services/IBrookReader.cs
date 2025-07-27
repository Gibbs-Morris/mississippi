using System.Collections.Immutable;

using Mississippi.Core.Abstractions.Brooks;


namespace Mississippi.Core.Brooks.Services;

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