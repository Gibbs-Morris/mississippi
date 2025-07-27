using Mississippi.EventSourcing.Abstractions;


namespace Mississippi.EventSourcing.Cosmos.Abstractions;

/// <summary>
///     Provides functionality for reading events from Cosmos DB event brooks.
/// </summary>
internal interface IEventBrookReader
{
    /// <summary>
    ///     Reads events from the specified brook range asynchronously.
    /// </summary>
    /// <param name="brookRange">The brook range specifying which events to read.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>An async enumerable of brook events within the specified range.</returns>
    IAsyncEnumerable<BrookEvent> ReadEventsAsync(
        BrookRangeKey brookRange,
        CancellationToken cancellationToken = default
    );
}