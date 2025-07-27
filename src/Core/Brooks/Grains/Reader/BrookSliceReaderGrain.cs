using System.Collections.Immutable;
using Mississippi.Core.Abstractions.Providers.Storage;
using Mississippi.Core.Abstractions.Streams;
using Mississippi.Core.Brooks.Grains.Factory;

namespace Mississippi.Core.Brooks.Grains.Reader;

internal class BrookSliceReaderGrain : IBrookSliceReaderGrain, IGrainBase
{
    public BrookSliceReaderGrain(
        IBrookStorageReader brookStorageReader, IGrainContext grainContext, IBrookGrainFactory brookGrainFactory)
    {
        BrookStorageReader = brookStorageReader;
        GrainContext = grainContext;
        BrookGrainFactory = brookGrainFactory;
    }

    private ImmutableArray<BrookEvent> Cache { get; set; } = ImmutableArray<BrookEvent>.Empty;

    private IBrookStorageReader BrookStorageReader { get; }

    public IGrainContext GrainContext { get; }
    private IBrookGrainFactory BrookGrainFactory { get; }

    private async Task PopulateCacheFromStreamAsync(
        BrookRangeKey brookRangeKey
    )
    {
        var l = new List<BrookEvent>();
        await foreach (var ev in BrookStorageReader.ReadEventsAsync(brookRangeKey))
        {
            l.Add(ev);
        }

        Cache = l.ToImmutableArray();
    }


    public async IAsyncEnumerable<BrookEvent> ReadAsync(BrookPosition minReadFrom, BrookPosition maxReadTo,
        CancellationToken cancellationToken = default)
    {
        BrookRangeKey brookRangeKey = this.GetPrimaryKeyString();
        var head = BrookGrainFactory.GetBrookHeadGrain(brookRangeKey.ToBrookCompositeKey());
        var lastPositionOfBrook = await head.GetLatestPositionAsync();
        var lastPositionOfSlice = brookRangeKey.End;
        BrookPosition lastPositionOfCache = (Cache.Length + brookRangeKey.Start);

        if (lastPositionOfCache < lastPositionOfSlice && lastPositionOfCache < lastPositionOfBrook)
        {
            await PopulateCacheFromStreamAsync(brookRangeKey);
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

    public async Task<ImmutableArray<BrookEvent>> ReadBatchAsync(BrookPosition minReadFrom, BrookPosition maxReadTo,
        CancellationToken cancellationToken = default)
    {
        List<BrookEvent> events = new();
        await foreach (var ev in ReadAsync(minReadFrom, maxReadTo, cancellationToken))
        {
            events.Add(ev);
        }

        return [..events];
    }
}