using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.EventSourcing.Snapshots.Cosmos.Storage;


namespace Mississippi.EventSourcing.Snapshots.Cosmos;

/// <summary>
///     Abstracts Cosmos container operations for snapshot storage.
/// </summary>
/// <remarks>
///     <para>
///         This interface follows the Interface Segregation Principle (ISP) by exposing only
///         the operations required for snapshot persistence, hiding Cosmos SDK implementation details.
///     </para>
///     <para>
///         Implementations handle retry policies, partition key construction, and document
///         ID formatting internally, keeping consumers focused on domain logic.
///     </para>
/// </remarks>
internal interface ISnapshotContainerOperations
{
    /// <summary>
    ///     Deletes a snapshot document by partition key and document ID.
    /// </summary>
    /// <param name="partitionKey">The partition key for the snapshot stream.</param>
    /// <param name="documentId">The document identifier (snapshot version).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that completes when deletion finishes; returns true if deleted, false if not found.</returns>
    Task<bool> DeleteDocumentAsync(
        string partitionKey,
        string documentId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Queries snapshot identifiers and versions for a given partition key.
    /// </summary>
    /// <param name="partitionKey">The partition key for the snapshot stream.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>An async enumerable of snapshot identifiers and versions.</returns>
    IAsyncEnumerable<SnapshotIdVersion> QuerySnapshotIdsAsync(
        string partitionKey,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Reads a snapshot document by partition key and document ID.
    /// </summary>
    /// <param name="partitionKey">The partition key for the snapshot stream.</param>
    /// <param name="documentId">The document identifier (snapshot version).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The snapshot document if found; otherwise null.</returns>
    Task<SnapshotDocument?> ReadDocumentAsync(
        string partitionKey,
        string documentId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Upserts a snapshot document.
    /// </summary>
    /// <param name="partitionKey">The partition key for the snapshot stream.</param>
    /// <param name="document">The snapshot document to persist.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous upsert.</returns>
    Task UpsertDocumentAsync(
        string partitionKey,
        SnapshotDocument document,
        CancellationToken cancellationToken = default
    );
}