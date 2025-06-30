using System.Collections.Immutable;


namespace Mississippi.Core.Idea.StreamGrains;

/// <summary>
///     Orleans grain contract that provides append (write) operations for an event stream.
/// </summary>
/// <remarks>
///     Implementations are keyed by the string returned from
///     <see cref="Mississippi.Core.Keys.StreamGrainKey" />, ensuring writes are scoped
///     to the correct event stream.
/// </remarks>
[Alias("Mississippi.Core.IStreamWriterGrain")]
public interface IStreamWriterGrain : IGrainWithStringKey
{
    /// <summary>
    ///     Appends a single event to the end of the stream.
    /// </summary>
    /// <param name="eventData">The event payload.</param>
    /// <param name="expectedVersion">
    ///     The version the caller believes the stream is at; used for optimistic concurrency. Pass
    ///     <c>-1</c> to skip the check.
    /// </param>
    /// <returns>A task that completes when the event has been persisted.</returns>
    [Alias("AppendEventAsync")]
    Task AppendEventAsync(
        MississippiEvent eventData,
        long expectedVersion = -1
    );

    /// <summary>
    ///     Appends one or more batches of events to the stream in a single write operation.
    /// </summary>
    /// <param name="events">An array of event batches to persist.</param>
    /// <param name="expectedVersion">
    ///     The version the caller believes the stream is at; used for optimistic concurrency. Pass
    ///     <c>-1</c> to skip the check.
    /// </param>
    /// <returns>A task that completes when all events have been persisted.</returns>
    [Alias("AppendEventsAsync")]
    Task AppendEventsAsync(
        ImmutableArray<MississippiEvent>[] events,
        long expectedVersion = -1
    );
}