using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.Brooks.Abstractions;


namespace Mississippi.Brooks.Runtime.Storage.Azure.Storage;

/// <summary>
///     Provides Azure Blob persistence operations for Brooks event storage.
/// </summary>
internal interface IAzureBrookRepository
{
    /// <summary>
    ///     Deletes an event blob when it exists.
    /// </summary>
    /// <param name="brookId">The brook identifier.</param>
    /// <param name="position">The event position.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    Task DeleteEventIfExistsAsync(
        BrookKey brookId,
        long position,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Checks whether the event blob exists for the specified position.
    /// </summary>
    /// <param name="brookId">The brook identifier.</param>
    /// <param name="position">The event position.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous existence check.</returns>
    Task<bool> EventExistsAsync(
        BrookKey brookId,
        long position,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Reads the committed cursor document for the specified brook.
    /// </summary>
    /// <param name="brookId">The brook identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous read operation.</returns>
    Task<AzureBrookCommittedCursorState?> GetCommittedCursorAsync(
        BrookKey brookId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Reads the pending-write document for the specified brook.
    /// </summary>
    /// <param name="brookId">The brook identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous read operation.</returns>
    Task<AzureBrookPendingWriteState?> GetPendingWriteAsync(
        BrookKey brookId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Reads committed event blobs for the specified range.
    /// </summary>
    /// <param name="brookRange">The committed range to read.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An async sequence of committed events.</returns>
    IAsyncEnumerable<BrookEvent> ReadEventsAsync(
        BrookRangeKey brookRange,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Advances the committed cursor using conditional blob writes.
    /// </summary>
    /// <param name="brookId">The brook identifier.</param>
    /// <param name="cursor">The last-seen committed cursor document.</param>
    /// <param name="pendingWrite">The pending write being committed.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous conditional update.</returns>
    Task<bool> TryAdvanceCommittedCursorAsync(
        BrookKey brookId,
        AzureBrookCommittedCursorState? cursor,
        AzureBrookPendingWriteState pendingWrite,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Creates a pending-write document when one does not already exist.
    /// </summary>
    /// <param name="brookId">The brook identifier.</param>
    /// <param name="pendingWrite">The pending write to create.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous conditional create.</returns>
    Task<bool> TryCreatePendingWriteAsync(
        BrookKey brookId,
        AzureBrookPendingWriteState pendingWrite,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Deletes the pending-write document using its last-seen ETag.
    /// </summary>
    /// <param name="brookId">The brook identifier.</param>
    /// <param name="pendingWrite">The pending write to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous conditional delete.</returns>
    Task<bool> TryDeletePendingWriteAsync(
        BrookKey brookId,
        AzureBrookPendingWriteState pendingWrite,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Writes an event blob for the specified position.
    /// </summary>
    /// <param name="brookId">The brook identifier.</param>
    /// <param name="position">The event position.</param>
    /// <param name="brookEvent">The event payload.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    Task WriteEventAsync(
        BrookKey brookId,
        long position,
        BrookEvent brookEvent,
        CancellationToken cancellationToken = default
    );
}