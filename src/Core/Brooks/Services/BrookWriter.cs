using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Mississippi.Core.Abstractions.Streams;
using Mississippi.Core.Brooks.Grains.Writer;

namespace Mississippi.Core.Brooks.Services;

public class BrookWriter : IBrookWriter
{
    private IGrainFactory GrainFactory { get; }

    public async Task<BrookPosition> AppendEventsAsync(BrookKey brookKey,
        ImmutableArray<BrookEvent> events,
        BrookPosition? expectedHeadPosition = null,
        CancellationToken cancellationToken = default)
    {
        var result = GrainFactory.GetGrain<IBrookWriterGrain>(brookKey);
        var newHeadPosition = await result.AppendEventsAsync(events, expectedHeadPosition, cancellationToken);
        return newHeadPosition;
    }
}