using System.Collections.Immutable;
using Mississippi.Core.Abstractions.Streams;
using Orleans.Concurrency;

namespace Mississippi.Core.Brooks.Grains.Reader;

// This is a internal grain which holds a slice to the stream, this means we can keep part in memory without having the whole thing, ie millions of events.
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