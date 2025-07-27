using System.Collections.Immutable;
using Mississippi.Core.Abstractions.Streams;

namespace Mississippi.Core.Brooks.Services;

// Public entry point.
public interface IBrookWriter
{
    Task<BrookPosition> AppendEventsAsync(
        BrookKey brookKey,
        ImmutableArray<BrookEvent> events,
        BrookPosition? expectedHeadPosition = null,
        CancellationToken cancellationToken = default
    );
}