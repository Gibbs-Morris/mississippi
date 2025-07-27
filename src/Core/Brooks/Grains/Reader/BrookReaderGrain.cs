using System.Collections.Immutable;
using Microsoft.Extensions.Options;
using Mississippi.Core.Abstractions.Streams;
using Mississippi.Core.Brooks.Grains.Factory;
using Orleans.Concurrency;

namespace Mississippi.Core.Brooks.Grains.Reader;

[StatelessWorker]
internal class BrookReaderGrain : IBrookReaderGrain, IGrainBase
{
    public BrookReaderGrain(
        IBrookGrainFactory brookGrainFactory,
        IOptions<BrookReaderOptions> options, IGrainContext grainContext)
    {
        BrookGrainFactory = brookGrainFactory;
        Options = options;
        GrainContext = grainContext;
    }

    private IOptions<BrookReaderOptions> Options { get; }

    private IBrookGrainFactory BrookGrainFactory { get; }

    public IGrainContext GrainContext { get; }


    public async IAsyncEnumerable<BrookEvent> ReadEventsAsync(BrookPosition? readFrom = null,
        BrookPosition? readTo = null,
        CancellationToken cancellationToken = default)
    {
        BrookKey brookId = this.GetPrimaryKeyString();
        BrookPosition start = 0;
        BrookPosition end = 0;

        start = readFrom ?? new BrookPosition(0);

        if (!readTo.HasValue)
        {
            var headGrain = BrookGrainFactory.GetBrookHeadGrain(brookId);
            end = await headGrain.GetLatestPositionAsync();
        }
        else
        {
            end = readTo.Value;
        }

        var sliceSize = Options.Value.StreamSliceSize;

        var baseIndexes = GetSliceReads(start.Value, end.Value, sliceSize);

        foreach (var l in baseIndexes)
        {
            var sliceGrain = BrookGrainFactory.GetBrookSliceReaderGrain(
                BrookRangeKey.FromBrookCompositeKey(brookId, l.BucketId, sliceSize));
            await foreach (var mississippiEvent in sliceGrain.ReadAsync(l.First, l.Last, cancellationToken))
            {
                yield return mississippiEvent;
            }
        }
    }

    public async Task<ImmutableArray<BrookEvent>> ReadEventsBatchAsync(BrookPosition? readFrom = null,
        BrookPosition? readTo = null,
        CancellationToken cancellationToken = default)
    {
        List<BrookEvent> events = new();
        await foreach (var ev in ReadEventsAsync(readFrom, readTo, cancellationToken))
        {
            events.Add(ev);
        }

        return [..events];
    }


    private static List<(long BucketId, long First, long Last)> GetSliceReads(long start, long end, long sliceSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sliceSize);
        if (start > end)
        {
            throw new ArgumentException("start must be ≤ end");
        }

        var first = (long)Math.Floor((double)start / sliceSize) * sliceSize;
        var last = (long)Math.Floor((double)end / sliceSize) * sliceSize;

        var result = new List<(long BucketId, long First, long Last)>();

        for (var b = first; b <= last; b += sliceSize)
        {
            var bucketId = b / sliceSize;
            var bucketFirst = Math.Max(b, start);
            var bucketLast = Math.Min(b + sliceSize - 1, end);

            result.Add((bucketId, bucketFirst, bucketLast));
        }

        return result;
    }
}