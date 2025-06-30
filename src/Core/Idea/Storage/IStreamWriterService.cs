using System.Collections.Immutable;

using Mississippi.Core.Keys;


namespace Mississippi.Core.Idea.Storage;

/// <summary>
///     Abstraction for appending events to persistent storage for a single event stream.
/// </summary>
/// <remarks>
///     Implementations MUST enforce optimistic concurrency using <paramref name="expectedVersion" />,
///     throwing an exception when the provided version does not match the current head.
/// </remarks>
public interface IStreamWriterService
{
    /// <summary>
    ///     Appends a single event to the stream.
    /// </summary>
    /// <param name="streamId">Stream identifier.</param>
    /// <param name="eventData">The event payload.</param>
    /// <param name="expectedVersion">Expected current stream version, or <c>-1</c> to bypass the check.</param>
    /// <returns>A task that completes when the event has been persisted.</returns>
    Task AppendEventAsync(
        StreamGrainKey streamId,
        MississippiEvent eventData,
        long expectedVersion = -1
    );

    /// <summary>
    ///     Appends a set of event batches to the stream atomically.
    /// </summary>
    /// <param name="streamId">Stream identifier.</param>
    /// <param name="events">Batches of events to write.</param>
    /// <param name="expectedVersion">Expected current stream version, or <c>-1</c> to bypass the check.</param>
    /// <returns>A task that completes when all events have been persisted.</returns>
    Task AppendEventsAsync(
        StreamGrainKey streamId,
        ImmutableArray<MississippiEvent>[] events,
        long expectedVersion = -1
    );
}