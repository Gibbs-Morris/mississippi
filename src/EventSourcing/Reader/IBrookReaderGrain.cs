using System.Collections.Immutable;

using Mississippi.EventSourcing.Abstractions;

using Orleans.Concurrency;


namespace Mississippi.EventSourcing.Reader;

/// <summary>
///     Main entry point grain for reading events from a brook (event stream).
///     This is a stateless grain that delegates to slice readers for actual event retrieval.
///     Provides both streaming and batch reading capabilities.
/// </summary>
[Alias("Mississippi.Core.IBrookReaderGrain")]
public interface IBrookReaderGrain : IGrainWithStringKey
{
    /// <summary>
    ///     Requests the reader grain to deactivate when idle, clearing any caches in its slice readers.
    /// </summary>
    /// <returns>A task that represents the asynchronous deactivation operation.</returns>
    [Alias("DeactivateAsync")]
    Task DeactivateAsync();

    /// <summary>
    ///     Reads events from the brook as an asynchronous stream.
    ///     Allows for efficient streaming of large numbers of events without loading them all into memory.
    /// </summary>
    /// <param name="readFrom">The starting position to read from. If null, reads from the beginning.</param>
    /// <param name="readTo">The ending position to read to. If null, reads to the end.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>An asynchronous enumerable of brook events in sequence order.</returns>
    [ReadOnly]
    [Alias("ReadEventsAsync")]
    IAsyncEnumerable<BrookEvent> ReadEventsAsync(
        BrookPosition? readFrom = null,
        BrookPosition? readTo = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Reads events from the brook and returns them as a batch collection.
    ///     Useful when you need to process all events at once or know the range is small.
    /// </summary>
    /// <param name="readFrom">The starting position to read from. If null, reads from the beginning.</param>
    /// <param name="readTo">The ending position to read to. If null, reads to the end.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>An immutable array containing all brook events in the specified range.</returns>
    [ReadOnly]
    [Alias("ReadEventsBatchAsync")]
    Task<ImmutableArray<BrookEvent>> ReadEventsBatchAsync(
        BrookPosition? readFrom = null,
        BrookPosition? readTo = null,
        CancellationToken cancellationToken = default
    );
}