using System.Collections.Immutable;

using Orleans.Concurrency;


namespace Mississippi.Core.Streams.Grains.Reader;

[Alias("Mississippi.Core.IStreamReaderGrain")]
public interface IStreamReaderGrain : IGrainWithStringKey
{
    [ReadOnly]
    [Alias("ReadEventsAsync")]
    IAsyncEnumerable<MississippiEvent> ReadEventsAsync(
        StreamRange range,
        CancellationToken cancellationToken = default
    );

    [ReadOnly]
    [Alias("ReadEventsBatchAsync")]
    Task<ImmutableArray<MississippiEvent>> ReadEventsBatchAsync(
        StreamRange range,
        CancellationToken cancellationToken = default
    );
}