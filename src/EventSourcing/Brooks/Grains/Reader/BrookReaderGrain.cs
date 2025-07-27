using System.Collections.Immutable;

using Microsoft.Extensions.Options;

using Mississippi.EventSourcing.Abstractions.Brooks;
using Mississippi.EventSourcing.Brooks.Grains.Factory;
using Mississippi.EventSourcing.Brooks.Grains.Head;

using Orleans.Concurrency;


namespace Mississippi.EventSourcing.Brooks.Grains.Reader;

[StatelessWorker]
internal class BrookReaderGrain
    : IBrookReaderGrain,
      IGrainBase
{
    public BrookReaderGrain(
        IBrookGrainFactory brookGrainFactory,
        IOptions<BrookReaderOptions> options,
        IGrainContext grainContext
    )
    {
        BrookGrainFactory = brookGrainFactory;
        Options = options;
        GrainContext = grainContext;
    }

    private IOptions<BrookReaderOptions> Options { get; }

    private IBrookGrainFactory BrookGrainFactory { get; }

    public async IAsyncEnumerable<BrookEvent> ReadEventsAsync(
        BrookPosition? readFrom = null,
        BrookPosition? readTo = null,
        CancellationToken cancellationToken = default
    )
    {
        BrookKey brookId = this.GetPrimaryKeyString();
        BrookPosition start = 0;
        BrookPosition end = 0;
        start = readFrom ?? new BrookPosition(0);
        if (!readTo.HasValue)
        {
            IBrookHeadGrain headGrain = BrookGrainFactory.GetBrookHeadGrain(brookId);
            end = await headGrain.GetLatestPositionAsync();
        }
        else
        {
            end = readTo.Value;
        }

        long sliceSize = Options.Value.BrookSliceSize;
        List<(long BucketId, long First, long Last)> baseIndexes = GetSliceReads(start.Value, end.Value, sliceSize);
        foreach ((long BucketId, long First, long Last) l in baseIndexes)
        {
            IBrookSliceReaderGrain sliceGrain = BrookGrainFactory.GetBrookSliceReaderGrain(
                BrookRangeKey.FromBrookCompositeKey(brookId, l.BucketId, sliceSize));
            await foreach (BrookEvent mississippiEvent in sliceGrain.ReadAsync(l.First, l.Last, cancellationToken))
            {
                yield return mississippiEvent;
            }
        }
    }

    public async Task<ImmutableArray<BrookEvent>> ReadEventsBatchAsync(
        BrookPosition? readFrom = null,
        BrookPosition? readTo = null,
        CancellationToken cancellationToken = default
    )
    {
        List<BrookEvent> events = new();
        await foreach (BrookEvent ev in ReadEventsAsync(readFrom, readTo, cancellationToken))
        {
            events.Add(ev);
        }

        return [..events];
    }

    public IGrainContext GrainContext { get; }

    private static List<(long BucketId, long First, long Last)> GetSliceReads(
        long start,
        long end,
        long sliceSize
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sliceSize);
        if (start > end)
        {
            throw new ArgumentException("start must be ≤ end");
        }

        long first = (long)Math.Floor((double)start / sliceSize) * sliceSize;
        long last = (long)Math.Floor((double)end / sliceSize) * sliceSize;
        List<(long BucketId, long First, long Last)> result = new();
        for (long b = first; b <= last; b += sliceSize)
        {
            long bucketId = b / sliceSize;
            long bucketFirst = Math.Max(b, start);
            long bucketLast = Math.Min((b + sliceSize) - 1, end);
            result.Add((bucketId, bucketFirst, bucketLast));
        }

        return result;
    }
}