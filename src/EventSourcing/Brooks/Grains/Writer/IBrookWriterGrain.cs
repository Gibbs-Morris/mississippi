using System.Collections.Immutable;

using Mississippi.EventSourcing.Abstractions.Brooks;
using Mississippi.EventSourcing.Brooks.Grains.Head;


namespace Mississippi.EventSourcing.Brooks.Grains.Writer;

/// <summary>
///     Orleans grain contract that provides append (write) operations for a Mississippi brook.
/// </summary>
/// <remarks>
///     Implementations are keyed by the string returned from
///     <see cref="BrookKey" />, ensuring writes are scoped
///     to the correct Mississippi brook.
/// </remarks>
[Alias("Mississippi.Core.IBrookWriterGrain")]
public interface IBrookWriterGrain : IGrainWithStringKey
{
    /// <summary>
    ///     Appends events to the Mississippi brook and publishes a <see cref="BrookHeadMovedEvent" /> on success.
    /// </summary>
    /// <param name="events">The events to append to the Mississippi brook.</param>
    /// <param name="expectedHeadPosition">Optional expected head position for optimistic concurrency.</param>
    /// <param name="cancellationToken">Token to observe cancellation requests.</param>
    /// <returns>The new brook head position after appending events.</returns>
    [Alias("AppendEventsAsync")]
    Task<BrookPosition> AppendEventsAsync(
        ImmutableArray<BrookEvent> events,
        BrookPosition? expectedHeadPosition = null,
        CancellationToken cancellationToken = default
    );
}