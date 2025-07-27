using System.Collections.Immutable;

using Mississippi.EventSourcing.Abstractions.Brooks;


namespace Mississippi.EventSourcing.Brooks.Services;

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