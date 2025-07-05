using System.Collections.Immutable;

using Mississippi.Core.Streams.StorageProvider;


namespace Mississippi.Core.Streams.Grains.Reader;

public class StreamSliceReaderGrain : IStreamSliceReaderGrain,IGrainBase
{
    public StreamSliceReaderGrain(
        IStreamStorageReader streamStorageReader, IGrainContext grainContext)
    {
        StreamStorageReader = streamStorageReader;
        GrainContext = grainContext;
    }

    private ImmutableDictionary<long, MississippiEvent> Cache { get; set; } =
        ImmutableDictionary<long, MississippiEvent>.Empty;

    private IStreamStorageReader StreamStorageReader { get; }

    public async IAsyncEnumerable<MississippiEvent> ReadStreamSliceAsync(
        StreamRange range,
        CancellationToken cancellationToken = default
    )
    {
        StreamCompositeRangeKey key = this.GetPrimaryKeyString();
        long expectedCount = key.Count;
        if (Cache.Count < expectedCount)
        {
            await PopulateCacheFromStreamAsync(key);
        }

        for (long v = range.Start; v <= (range.Start + range.Count); v++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            if (Cache.TryGetValue(v, out MississippiEvent? cachedEvent))
            {
                yield return cachedEvent;
            }
            else
            {
                throw new InvalidOperationException($"Event at version {v} not found in cache");
            }
        }
    }

    public async Task<ImmutableArray<MississippiEvent>> ReadStreamSliceBatchAsync(
        StreamRange range,
        CancellationToken cancellationToken = default
    )
    {
        List<MississippiEvent> events = new();
        await foreach (MississippiEvent ev in ReadStreamSliceAsync(range, cancellationToken))
        {
            events.Add(ev);
        }

        return [..events];
    }

    private async Task PopulateCacheFromStreamAsync(
        StreamCompositeRangeKey key
    )
    {
        long version = key.Start;
        await foreach (MississippiEvent ev in StreamStorageReader.ReadEventsAsync(key))
        {
            Cache = Cache.SetItem(version, ev);
            version++;
        }
    }

    public IGrainContext GrainContext { get; }
}