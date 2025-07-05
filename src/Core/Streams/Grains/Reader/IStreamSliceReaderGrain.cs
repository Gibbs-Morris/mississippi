using System.Collections.Immutable;

using Orleans.Concurrency;


namespace Mississippi.Core.Streams.Grains.Reader;

[Alias("Mississippi.Core.IStreamSliceReaderGrain")]
public interface IStreamSliceReaderGrain : IGrainWithStringKey
{
    [ReadOnly]
    [Alias("ReadStreamSliceAsync")]
    IAsyncEnumerable<MississippiEvent> ReadStreamSliceAsync(
        StreamRange range,
        CancellationToken cancellationToken = default
    );

    [ReadOnly]
    [Alias("ReadStreamSliceBatchAsync")]
    Task<ImmutableArray<MississippiEvent>> ReadStreamSliceBatchAsync(
        StreamRange range,
        CancellationToken cancellationToken = default
    );
}