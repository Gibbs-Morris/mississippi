using Microsoft.Azure.Cosmos;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Cosmos.Storage;


namespace Mississippi.EventSourcing.Cosmos.Abstractions;

/// <summary>
///     Provides low-level Cosmos DB operations for event brook management.
/// </summary>
internal interface ICosmosRepository
{
    /// <summary>
    ///     Appends a batch of events to the brook starting at the specified position.
    /// </summary>
    /// <param name="brookId">The brook identifier specifying the target brook.</param>
    /// <param name="events">The collection of events to append.</param>
    /// <param name="startPosition">The starting position for the first event in the batch.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AppendEventBatchAsync(
        BrookKey brookId,
        IReadOnlyList<EventStorageModel> events,
        long startPosition,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Commits the head position by updating the main head document and removing the pending head.
    /// </summary>
    /// <param name="brookId">The brook identifier specifying the target brook.</param>
    /// <param name="finalPosition">The final position to commit.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CommitHeadPositionAsync(
        BrookKey brookId,
        long finalPosition,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Creates a pending head document for optimistic concurrency control during batch operations.
    /// </summary>
    /// <param name="brookId">The brook identifier specifying the target brook.</param>
    /// <param name="currentHead">The current head position before the operation.</param>
    /// <param name="finalPosition">The expected final position after the operation.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CreatePendingHeadAsync(
        BrookKey brookId,
        BrookPosition currentHead,
        long finalPosition,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Deletes an event at the specified position from the brook.
    /// </summary>
    /// <param name="brookId">The brook identifier specifying the target brook.</param>
    /// <param name="position">The position of the event to delete.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteEventAsync(
        BrookKey brookId,
        long position,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Deletes the pending head document for the specified brook.
    /// </summary>
    /// <param name="brookId">The brook identifier specifying the target brook.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeletePendingHeadAsync(
        BrookKey brookId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Checks if an event exists at the specified position in the brook.
    /// </summary>
    /// <param name="brookId">The brook identifier specifying the target brook.</param>
    /// <param name="position">The position to check for event existence.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>True if the event exists, otherwise false.</returns>
    Task<bool> EventExistsAsync(
        BrookKey brookId,
        long position,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Executes a transactional batch operation for appending events with optimistic concurrency control.
    /// </summary>
    /// <param name="brookId">The brook identifier specifying the target brook.</param>
    /// <param name="events">The collection of events to append in the transaction.</param>
    /// <param name="currentHead">The current head position before the operation.</param>
    /// <param name="newPosition">The new head position after the operation.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The transactional batch response from Cosmos DB.</returns>
    Task<TransactionalBatchResponse> ExecuteTransactionalBatchAsync(
        BrookKey brookId,
        IReadOnlyList<EventStorageModel> events,
        BrookPosition currentHead,
        long newPosition,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Gets the set of existing event positions within the specified range.
    /// </summary>
    /// <param name="brookId">The brook identifier specifying the target brook.</param>
    /// <param name="startPosition">The start position of the range (inclusive).</param>
    /// <param name="endPosition">The end position of the range (inclusive).</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A set of positions where events exist.</returns>
    Task<ISet<long>> GetExistingEventPositionsAsync(
        BrookKey brookId,
        long startPosition,
        long endPosition,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Gets the head document for the specified brook.
    /// </summary>
    /// <param name="brookId">The brook identifier specifying the target brook.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The head storage model if found, otherwise null.</returns>
    Task<HeadStorageModel?> GetHeadDocumentAsync(
        BrookKey brookId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Gets the pending head document for the specified brook.
    /// </summary>
    /// <param name="brookId">The brook identifier specifying the target brook.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The pending head storage model if found, otherwise null.</returns>
    Task<HeadStorageModel?> GetPendingHeadDocumentAsync(
        BrookKey brookId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Queries events from the specified brook range with the given batch size.
    /// </summary>
    /// <param name="brookRange">The brook range specifying which events to query.</param>
    /// <param name="batchSize">The batch size for pagination.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>An async enumerable of event storage models within the range.</returns>
    IAsyncEnumerable<EventStorageModel> QueryEventsAsync(
        BrookRangeKey brookRange,
        int batchSize,
        CancellationToken cancellationToken = default
    );
}