using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.Brooks.Abstractions.Streaming;

using Orleans;


namespace Mississippi.Brooks.Abstractions.Writer;

/// <summary>
///     Orleans grain contract that provides append (write) operations for a Mississippi brook.
/// </summary>
/// <remarks>
///     Implementations are keyed by the string returned from
///     <see cref="BrookKey" />, ensuring writes are scoped
///     to the correct Mississippi brook.
/// </remarks>
[Alias("Mississippi.Brooks.Abstractions.Writer.IBrookWriterGrain")]
public interface IBrookWriterGrain : IGrainWithStringKey
{
    /// <summary>
    ///     Appends events to the Mississippi brook and publishes a <see cref="BrookCursorMovedEvent" /> on success.
    /// </summary>
    /// <param name="events">The events to append to the Mississippi brook.</param>
    /// <param name="expectedCursorPosition">Optional expected cursor position for optimistic concurrency.</param>
    /// <param name="cancellationToken">Token to observe cancellation requests.</param>
    /// <returns>The new brook cursor position after appending events.</returns>
    /// <exception cref="BrookCursorPublicationException">
    ///     The events were committed, but cursor publication failed. Retry publication without appending again.
    /// </exception>
    [Alias("AppendEventsAsync")]
    Task<BrookPosition> AppendEventsAsync(
        ImmutableArray<BrookEvent> events,
        BrookPosition? expectedCursorPosition = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Publishes a confirmed committed cursor position without appending events.
    /// </summary>
    /// <param name="position">The position confirmed by append or authoritative storage recovery.</param>
    /// <param name="cancellationToken">Token to observe before publishing the cursor update.</param>
    /// <returns>A task representing publication of the cursor update.</returns>
    [Alias("PublishCursorAsync")]
    Task PublishCursorAsync(
        BrookPosition position,
        CancellationToken cancellationToken = default
    );
}