using System.Collections.Immutable;

using Mississippi.Core.Abstractions.Brooks;
using Mississippi.Core.Brooks.Grains.Writer;


namespace Mississippi.Core.Brooks.Services;

public class BrookWriter : IBrookWriter
{
    private IGrainFactory GrainFactory { get; }

    public async Task<BrookPosition> AppendEventsAsync(
        BrookKey brookKey,
        ImmutableArray<BrookEvent> events,
        BrookPosition? expectedHeadPosition = null,
        CancellationToken cancellationToken = default
    )
    {
        IBrookWriterGrain? result = GrainFactory.GetGrain<IBrookWriterGrain>(brookKey);
        BrookPosition newHeadPosition = await result.AppendEventsAsync(events, expectedHeadPosition, cancellationToken);
        return newHeadPosition;
    }
}