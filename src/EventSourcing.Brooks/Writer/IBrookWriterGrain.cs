using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Cursor;

using Orleans;


namespace Mississippi.EventSourcing.Brooks.Writer;

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
    ///     Appends events to the Mississippi brook and publishes a <see cref="BrookCursorMovedEvent" /> on success.
    /// </summary>
    /// <param name="events">The events to append to the Mississippi brook.</param>
    /// <param name="expectedCursorPosition">Optional expected cursor position for optimistic concurrency.</param>
    /// <param name="cancellationToken">Token to observe cancellation requests.</param>
    /// <returns>The new brook cursor position after appending events.</returns>
    [Alias("AppendEventsAsync")]
    Task<BrookPosition> AppendEventsAsync(
        ImmutableArray<BrookEvent> events,
        BrookPosition? expectedCursorPosition = null,
        CancellationToken cancellationToken = default
    );
}