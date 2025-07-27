using System.Collections.Immutable;

using Mississippi.Core.Abstractions.Brooks;

using Orleans.Concurrency;


namespace Mississippi.Core.Brooks.Grains.Reader;

// This is the main entry point for reading events from a brook.
// This holds no state it always goes to the slice reader for it.
[Alias("Mississippi.Core.IBrookReaderGrain")]
public interface IBrookReaderGrain : IGrainWithStringKey
{
    [ReadOnly]
    [Alias("ReadEventsAsync")]
    IAsyncEnumerable<BrookEvent> ReadEventsAsync(
        BrookPosition? readFrom = null,
        BrookPosition? readTo = null,
        CancellationToken cancellationToken = default
    );

    [ReadOnly]
    [Alias("ReadEventsBatchAsync")]
    Task<ImmutableArray<BrookEvent>> ReadEventsBatchAsync(
        BrookPosition? readFrom = null,
        BrookPosition? readTo = null,
        CancellationToken cancellationToken = default
    );
}