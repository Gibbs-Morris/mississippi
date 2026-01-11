using Microsoft.Extensions.Logging;


namespace Mississippi.EventSourcing.Snapshots.Cosmos.Storage;

/// <summary>
///     High-performance logging extensions for snapshot storage operations.
/// </summary>
internal static partial class SnapshotStorageLoggerExtensions
{
    /// <summary>
    ///     Logs completion of deleting all snapshots for a stream.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Deleted {Count} snapshots for stream partition '{PartitionKey}'")]
    public static partial void DeletedAllSnapshots(
        this ILogger logger,
        string partitionKey,
        int count
    );

    // -------------------------
    // Repository Operations
    // -------------------------

    /// <summary>
    ///     Logs the start of deleting all snapshots for a stream.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Deleting all snapshots for stream partition '{PartitionKey}'")]
    public static partial void DeletingAllSnapshots(
        this ILogger logger,
        string partitionKey
    );

    /// <summary>
    ///     Logs a document delete attempt.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Deleting snapshot document '{DocumentId}' from partition '{PartitionKey}'")]
    public static partial void DeletingDocument(
        this ILogger logger,
        string partitionKey,
        string documentId
    );

    /// <summary>
    ///     Logs successful document deletion.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Deleted snapshot document '{DocumentId}' from partition '{PartitionKey}'")]
    public static partial void DocumentDeleted(
        this ILogger logger,
        string partitionKey,
        string documentId
    );

    /// <summary>
    ///     Logs a successful document read.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Found snapshot document '{DocumentId}' in partition '{PartitionKey}'")]
    public static partial void DocumentFound(
        this ILogger logger,
        string partitionKey,
        string documentId
    );

    /// <summary>
    ///     Logs a document not found result.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Snapshot document '{DocumentId}' not found in partition '{PartitionKey}'")]
    public static partial void DocumentNotFound(
        this ILogger logger,
        string partitionKey,
        string documentId
    );

    /// <summary>
    ///     Logs document not found during delete (not an error).
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Snapshot document '{DocumentId}' not found for deletion in partition '{PartitionKey}'")]
    public static partial void DocumentNotFoundForDeletion(
        this ILogger logger,
        string partitionKey,
        string documentId
    );

    /// <summary>
    ///     Logs successful document upsert.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Successfully upserted snapshot document to partition '{PartitionKey}'")]
    public static partial void DocumentUpserted(
        this ILogger logger,
        string partitionKey
    );

    /// <summary>
    ///     Logs no snapshots found during prune.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "No snapshots found to prune for stream partition '{PartitionKey}'")]
    public static partial void NoSnapshotsToPrune(
        this ILogger logger,
        string partitionKey
    );

    /// <summary>
    ///     Logs completion of a prune operation.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message =
            "Pruned {DeletedCount} snapshots, retained {RetainedCount} for stream partition '{PartitionKey}' (max version: {MaxVersion})")]
    public static partial void PrunedSnapshots(
        this ILogger logger,
        string partitionKey,
        int deletedCount,
        int retainedCount,
        long maxVersion
    );

    /// <summary>
    ///     Logs the start of a prune operation.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Pruning snapshots for stream partition '{PartitionKey}' with {ModuliCount} retain moduli")]
    public static partial void PruningSnapshots(
        this ILogger logger,
        string partitionKey,
        int moduliCount
    );

    /// <summary>
    ///     Logs the start of querying snapshot IDs.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Debug, Message = "Querying snapshot IDs for partition '{PartitionKey}'")]
    public static partial void QueryingSnapshotIds(
        this ILogger logger,
        string partitionKey
    );

    // -------------------------
    // Container Operations
    // -------------------------

    /// <summary>
    ///     Logs a document read attempt.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Reading snapshot document '{DocumentId}' from partition '{PartitionKey}'")]
    public static partial void ReadingDocument(
        this ILogger logger,
        string partitionKey,
        string documentId
    );

    /// <summary>
    ///     Logs a page of snapshot IDs retrieved.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Retrieved {Count} snapshot IDs in page for partition '{PartitionKey}'")]
    public static partial void SnapshotIdsPageRetrieved(
        this ILogger logger,
        string partitionKey,
        int count
    );

    /// <summary>
    ///     Logs a document upsert.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Debug, Message = "Upserting snapshot document to partition '{PartitionKey}'")]
    public static partial void UpsertingDocument(
        this ILogger logger,
        string partitionKey
    );
}