using Mississippi.Core.Abstractions.Streams;

namespace Mississippi.Core.Abstractions.Providers.Storage;

/// <summary>
/// Provides read access to brook event streams.
/// </summary>
public interface IBrookStorageReader
{
    /// <summary>
    /// Reads the current head position for a brook stream.
    /// </summary>
    /// <param name="brookId">The identifier of the brook stream.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The current head position of the stream.</returns>
    Task<BrookPosition> ReadHeadPositionAsync(
        BrookKey brookId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Reads events from a brook stream within the specified range.
    /// </summary>
    /// <param name="brookRange">The range specification for the events to read.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>An async enumerable of brook events within the specified range.</returns>
    IAsyncEnumerable<BrookEvent> ReadEventsAsync(
        BrookRangeKey brookRange,
        CancellationToken cancellationToken = default
    );
}