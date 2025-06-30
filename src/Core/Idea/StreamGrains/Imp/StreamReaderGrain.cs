using Mississippi.Core.Keys;


namespace Mississippi.Core.Idea.StreamGrains.Imp;

public class StreamReaderGrain : IStreamReaderGrain
{
    public StreamReaderGrain(
        IEventSourceGrainFactory eventSourceGrainFactory
    )
    {
        EventSourceGrainFactory = eventSourceGrainFactory;
    }

    private IEventSourceGrainFactory EventSourceGrainFactory { get; }

    public async IAsyncEnumerable<MississippiEvent> ReadEventsAsync(
        long? fromVersion = null,
        long? toVersion = null
    )
    {
        long startVersion = fromVersion.HasValue && (fromVersion.Value > 0) ? fromVersion.Value : 1;
        string? streamId = this.GetPrimaryKeyString();
        StreamGrainKey streamKey = new()
        {
            Id = streamId,
        };
        long endVersion;
        if (toVersion.HasValue && (toVersion.Value > 0))
        {
            endVersion = toVersion.Value;
        }
        else
        {
            IStreamHeadGrain headGrain = EventSourceGrainFactory.GetStreamHeadGrain(streamKey);
            endVersion = await headGrain.GetLatestVersionAsync();
        }

        const int sliceSize = 100;
        for (long start = startVersion; start <= endVersion; start += sliceSize)
        {
            long end = Math.Min((start + sliceSize) - 1, endVersion);
            StreamSliceGrainKey sliceKey = new()
            {
                Id = streamId,
                FromVersion = start,
                ToVersion = end,
            };
            IStreamSliceReaderGrain sliceGrain = EventSourceGrainFactory.GetStreamSliceGrain(sliceKey);
            await foreach (MississippiEvent ev in sliceGrain.ReadStreamSliceAsync())
            {
                yield return ev;
            }
        }
    }
}