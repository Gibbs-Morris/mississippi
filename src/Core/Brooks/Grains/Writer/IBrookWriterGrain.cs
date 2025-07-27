using System.Collections.Immutable;
using Mississippi.Core.Abstractions.Streams;
using Mississippi.Core.Brooks.Grains.Head;

namespace Mississippi.Core.Brooks.Grains.Writer;

/// <summary>
///     Orleans grain contract that provides append (write) operations for a Mississippi event stream.
/// </summary>
/// <remarks>
///     Implementations are keyed by the string returned from
///     <see cref="BrookKey" />, ensuring writes are scoped
///     to the correct Mississippi event stream.
/// </remarks>
[Alias("Mississippi.Core.IBrookWriterGrain")]
public interface IBrookWriterGrain : IGrainWithStringKey
{
    /// <summary>
    ///     Appends events to the Mississippi event stream and publishes a <see cref="StreamHeadMovedEvent" /> on success.
    /// </summary>
    /// <param name="events">The events to append to the Mississippi event stream.</param>
    /// <param name="expectedHeadPosition">Optional expected head position for optimistic concurrency.</param>
    /// <param name="cancellationToken">Token to observe cancellation requests.</param>
    /// <returns>The new stream head position after appending events.</returns>
    [Alias("AppendEventsAsync")]
    Task<BrookPosition> AppendEventsAsync(
        ImmutableArray<BrookEvent> events,
        BrookPosition? expectedHeadPosition = null,
        CancellationToken cancellationToken = default
    );
}