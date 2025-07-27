using System.Collections.Immutable;

using Mississippi.EventSourcing.Abstractions.Brooks;
using Mississippi.EventSourcing.Brooks.Grains.Reader;


namespace Mississippi.EventSourcing.Brooks.Services;

public class BrookReader : IBrookReader
{
    public BrookReader(
        IGrainFactory grainFactory
    ) =>
        GrainFactory = grainFactory;

    private IGrainFactory GrainFactory { get; }

    public IAsyncEnumerable<BrookEvent> ReadEventsAsync(
        BrookKey brookKey,
        BrookPosition? readFrom = null,
        BrookPosition? readTo = null,
        CancellationToken cancellationToken = default
    )
    {
        IBrookReaderGrain? result = GrainFactory.GetGrain<IBrookReaderGrain>(brookKey);
        return result.ReadEventsAsync(readFrom, readTo, cancellationToken);
    }

    public Task<ImmutableArray<BrookEvent>> ReadEventsBatchAsync(
        BrookKey brookKey,
        BrookPosition? readFrom = null,
        BrookPosition? readTo = null,
        CancellationToken cancellationToken = default
    )
    {
        IBrookReaderGrain? result = GrainFactory.GetGrain<IBrookReaderGrain>(brookKey);
        return result.ReadEventsBatchAsync(readFrom, readTo, cancellationToken);
    }
}