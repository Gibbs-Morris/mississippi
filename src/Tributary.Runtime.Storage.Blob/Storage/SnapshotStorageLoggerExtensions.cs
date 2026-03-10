using Microsoft.Extensions.Logging;


namespace Mississippi.Tributary.Runtime.Storage.Blob.Storage;

/// <summary>
///     High-performance logging extensions for Blob snapshot storage operations.
/// </summary>
internal static partial class SnapshotStorageLoggerExtensions
{
    /// <summary>
    ///     Logs completion of deleting all Blob snapshots for a prefix.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Deleted {Count} Blob snapshots for prefix '{Prefix}'")]
    public static partial void DeletedAllSnapshots(
        this ILogger logger,
        string prefix,
        int count
    );

    /// <summary>
    ///     Logs the start of deleting all Blob snapshots for a prefix.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Deleting all Blob snapshots for prefix '{Prefix}'")]
    public static partial void DeletingAllSnapshots(
        this ILogger logger,
        string prefix
    );

    /// <summary>
    ///     Logs a Blob delete attempt.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Debug, Message = "Deleting Blob '{BlobName}'")]
    public static partial void DeletingBlob(
        this ILogger logger,
        string blobName
    );

    /// <summary>
    ///     Logs Blob delete results.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Debug, Message = "Blob '{BlobName}' deleted: {Deleted}")]
    public static partial void BlobDeleted(
        this ILogger logger,
        string blobName,
        bool deleted
    );

    /// <summary>
    ///     Logs when a Blob was found.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Debug, Message = "Blob '{BlobName}' found")]
    public static partial void BlobFound(
        this ILogger logger,
        string blobName
    );

    /// <summary>
    ///     Logs when a Blob was not found.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Debug, Message = "Blob '{BlobName}' not found")]
    public static partial void BlobNotFound(
        this ILogger logger,
        string blobName
    );

    /// <summary>
    ///     Logs the start of container initialization.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Ensuring Blob container '{ContainerName}' exists")]
    public static partial void EnsuringContainerExists(
        this ILogger logger,
        string containerName
    );

    /// <summary>
    ///     Logs completion of container initialization.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Container '{ContainerName}' ensured for Blob snapshot storage")]
    public static partial void ContainerEnsured(
        this ILogger logger,
        string containerName
    );

    /// <summary>
    ///     Logs when no snapshots were found to prune.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "No Blob snapshots found to prune for prefix '{Prefix}'")]
    public static partial void NoSnapshotsToPrune(
        this ILogger logger,
        string prefix
    );

    /// <summary>
    ///     Logs completion of a prune operation.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Pruned {DeletedCount} Blob snapshots, retained {RetainedCount} for prefix '{Prefix}' (max version: {MaxVersion})")]
    public static partial void PrunedSnapshots(
        this ILogger logger,
        string prefix,
        int deletedCount,
        int retainedCount,
        long maxVersion
    );

    /// <summary>
    ///     Logs the start of a prune operation.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Pruning Blob snapshots for prefix '{Prefix}' with {ModuliCount} retain moduli")]
    public static partial void PruningSnapshots(
        this ILogger logger,
        string prefix,
        int moduliCount
    );

    /// <summary>
    ///     Logs the start of listing Blob snapshots.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Listing Blob snapshots with prefix '{Prefix}'")]
    public static partial void ListingBlobs(
        this ILogger logger,
        string prefix
    );

    /// <summary>
    ///     Logs the start of downloading a Blob snapshot.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Debug, Message = "Downloading Blob snapshot '{BlobName}'")]
    public static partial void DownloadingBlob(
        this ILogger logger,
        string blobName
    );

    /// <summary>
    ///     Logs the start of uploading a Blob snapshot.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Debug, Message = "Uploading Blob snapshot '{BlobName}'")]
    public static partial void UploadingBlob(
        this ILogger logger,
        string blobName
    );

    /// <summary>
    ///     Logs successful Blob snapshot upload.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Debug, Message = "Uploaded Blob snapshot '{BlobName}'")]
    public static partial void BlobUploaded(
        this ILogger logger,
        string blobName
    );
}
