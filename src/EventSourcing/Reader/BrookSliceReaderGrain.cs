using System.Collections.Immutable;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Abstractions.Storage;
using Mississippi.EventSourcing.Factory;
using Mississippi.EventSourcing.Head;


namespace Mississippi.EventSourcing.Reader;

internal class BrookSliceReaderGrain
    : IBrookSliceReaderGrain,
      IGrainBase
{
    public BrookSliceReaderGrain(
        IBrookStorageReader brookStorageReader,
        IGrainContext grainContext,
        IBrookGrainFactory brookGrainFactory
    )
    {
        BrookStorageReader = brookStorageReader;
        GrainContext = grainContext;
        BrookGrainFactory = brookGrainFactory;
    }

    private ImmutableArray<BrookEvent> Cache { get; set; } = ImmutableArray<BrookEvent>.Empty;

    private IBrookStorageReader BrookStorageReader { get; }

    private IBrookGrainFactory BrookGrainFactory { get; }

    public async IAsyncEnumerable<BrookEvent> ReadAsync(
        BrookPosition minReadFrom,
        BrookPosition maxReadTo,
        CancellationToken cancellationToken = default
    )
    {
        BrookRangeKey brookRangeKey = this.GetPrimaryKeyString();
        IBrookHeadGrain head = BrookGrainFactory.GetBrookHeadGrain(brookRangeKey.ToBrookCompositeKey());
        BrookPosition lastPositionOfBrook = await head.GetLatestPositionAsync();
        BrookPosition lastPositionOfSlice = brookRangeKey.End;
        BrookPosition lastPositionOfCache = Cache.Length + brookRangeKey.Start;
        if ((lastPositionOfCache < lastPositionOfSlice) && (lastPositionOfCache < lastPositionOfBrook))
        {
            await PopulateCacheFromBrookAsync(brookRangeKey);
        }

        for (int i = 0; i < Cache.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            BrookPosition position = brookRangeKey.Start + i; // derive absolute position
            if (position < minReadFrom)
            {
                continue;
            }

            if (position > maxReadTo)
            {
                break; // cache is ordered, safe to stop
            }

            yield return Cache[i];
        }
    }

    public async Task<ImmutableArray<BrookEvent>> ReadBatchAsync(
        BrookPosition minReadFrom,
        BrookPosition maxReadTo,
        CancellationToken cancellationToken = default
    )
    {
        List<BrookEvent> events = new();
        await foreach (BrookEvent ev in ReadAsync(minReadFrom, maxReadTo, cancellationToken))
        {
            events.Add(ev);
        }

        return [..events];
    }

    public IGrainContext GrainContext { get; }

    private async Task PopulateCacheFromBrookAsync(
        BrookRangeKey brookRangeKey
    )
    {
        List<BrookEvent> l = new();
        await foreach (BrookEvent ev in BrookStorageReader.ReadEventsAsync(brookRangeKey))
        {
            l.Add(ev);
        }

        Cache = l.ToImmutableArray();
    }
}