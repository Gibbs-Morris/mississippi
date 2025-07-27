using System.Collections.Immutable;

using Mississippi.EventSourcing.Abstractions.Brooks;

using Orleans.Concurrency;


namespace Mississippi.EventSourcing.Brooks.Grains.Reader;

// This is a internal grain which holds a slice to the brook, this means we can keep part in memory without having the whole thing, ie millions of events.
[Alias("Mississippi.Core.IBrookSliceReaderGrain")]
public interface IBrookSliceReaderGrain : IGrainWithStringKey
{
    [ReadOnly]
    [Alias("ReadAsync")]
    IAsyncEnumerable<BrookEvent> ReadAsync(
        BrookPosition minReadFrom,
        BrookPosition maxReadTo,
        CancellationToken cancellationToken = default
    );

    [ReadOnly]
    [Alias("ReadBatchAsync")]
    Task<ImmutableArray<BrookEvent>> ReadBatchAsync(
        BrookPosition minReadFrom,
        BrookPosition maxReadTo,
        CancellationToken cancellationToken = default
    );
}