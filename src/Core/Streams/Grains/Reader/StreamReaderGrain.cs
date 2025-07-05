using System.Collections.Immutable;

using Microsoft.Extensions.Options;

using Mississippi.Core.Streams.Grains.Factory;
using Mississippi.Core.Streams.Grains.Head;


namespace Mississippi.Core.Streams.Grains.Reader;

public class StreamReaderGrain : IStreamReaderGrain,IGrainBase
{
    public StreamReaderGrain(
        IStreamGrainFactory streamGrainFactory,
        IOptions<StreamReaderOptions> options, IGrainContext grainContext)
    {
        StreamGrainFactory = streamGrainFactory;
        Options = options;
        GrainContext = grainContext;
    }

    private IOptions<StreamReaderOptions> Options { get; }

    private IStreamGrainFactory StreamGrainFactory { get; }

    public async IAsyncEnumerable<MississippiEvent> ReadEventsAsync(
        StreamRange range,
        CancellationToken cancellationToken = default
    )
    {
        StreamCompositeKey streamId = this.GetPrimaryKeyString();
        long endVersion;
        if (range.Count != null)
        {
            endVersion = range.Start.Value + range.Count.Value;
        }
        else
        {
            IStreamHeadGrain headGrain = StreamGrainFactory.GetStreamHeadGrain(streamId);
            endVersion = await headGrain.GetLatestPositionAsync();
        }

        int sliceSize = Options.Value.StreamSliceSize;
        for (long start = range.Start.Value; start <= endVersion; start += sliceSize)
        {
            long count = Math.Min((start + sliceSize) - 1, endVersion);
            IStreamSliceReaderGrain sliceGrain = StreamGrainFactory.GetStreamSliceGrain(
                StreamCompositeRangeKey.FromStreamCompositeKey(streamId, start, count));
            await foreach (MississippiEvent ev in sliceGrain.ReadStreamSliceAsync(range, cancellationToken))
            {
                yield return ev;
            }
        }
    }

    public async Task<ImmutableArray<MississippiEvent>> ReadEventsBatchAsync(
        StreamRange range,
        CancellationToken cancellationToken = default
    )
    {
        List<MississippiEvent> events = new();
        await foreach (MississippiEvent ev in ReadEventsAsync(range, cancellationToken))
        {
            events.Add(ev);
        }

        return [..events];
    }

    public IGrainContext GrainContext { get; }
}