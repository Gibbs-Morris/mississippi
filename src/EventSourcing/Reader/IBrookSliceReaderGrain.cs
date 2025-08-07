using System.Collections.Immutable;

using Mississippi.EventSourcing.Abstractions;

using Orleans.Concurrency;


namespace Mississippi.EventSourcing.Reader;

/// <summary>
///     Internal grain that manages a slice of a brook (event stream) for memory-efficient reading.
///     Keeps a portion of the brook in memory rather than loading millions of events at once.
///     Used internally by the main brook reader grain to handle large event streams.
/// </summary>
[Alias("Mississippi.Core.IBrookSliceReaderGrain")]
public interface IBrookSliceReaderGrain : IGrainWithStringKey
{
    /// <summary>
    ///     Reads events from this brook slice as an asynchronous stream.
    ///     Only returns events within the slice's managed range.
    /// </summary>
    /// <param name="minReadFrom">The minimum position to start reading from within this slice.</param>
    /// <param name="maxReadTo">The maximum position to read to within this slice.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>An asynchronous enumerable of brook events within the specified range.</returns>
    [ReadOnly]
    [Alias("ReadAsync")]
    IAsyncEnumerable<BrookEvent> ReadAsync(
        BrookPosition minReadFrom,
        BrookPosition maxReadTo,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Reads events from this brook slice and returns them as a batch collection.
    ///     Only returns events within the slice's managed range.
    /// </summary>
    /// <param name="minReadFrom">The minimum position to start reading from within this slice.</param>
    /// <param name="maxReadTo">The maximum position to read to within this slice.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>An immutable array containing brook events within the specified range.</returns>
    [ReadOnly]
    [Alias("ReadBatchAsync")]
    Task<ImmutableArray<BrookEvent>> ReadBatchAsync(
        BrookPosition minReadFrom,
        BrookPosition maxReadTo,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Requests the slice reader to deactivate when idle, clearing its in-memory cache.
    /// </summary>
    [Alias("DeactivateAsync")]
    Task DeactivateAsync();
}