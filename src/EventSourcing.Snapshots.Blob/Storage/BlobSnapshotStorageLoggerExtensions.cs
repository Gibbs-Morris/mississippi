using Microsoft.Extensions.Logging;


namespace Mississippi.EventSourcing.Snapshots.Blob.Storage;

/// <summary>
///     High-performance logging extensions for blob storage operations.
/// </summary>
internal static partial class BlobSnapshotStorageLoggerExtensions
{
    /// <summary>
    ///     Logs when a batch delete had partial failures.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="totalBlobs">The total number of blobs in the batch.</param>
    /// <param name="failureCount">The number of failures.</param>
    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Warning,
        Message = "Batch delete had partial failures: {FailureCount} of {TotalBlobs} failed")]
    public static partial void BatchDeletePartialFailure(
        this ILogger logger,
        int totalBlobs,
        int failureCount
    );

    /// <summary>
    ///     Logs when attempting to delete a blob that was already deleted.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="blobPath">The blob path.</param>
    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "Blob already deleted: '{BlobPath}'")]
    public static partial void BlobAlreadyDeleted(
        this ILogger logger,
        string blobPath
    );

    /// <summary>
    ///     Logs when a blob was not found (404).
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="blobPath">The blob path.</param>
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Blob not found: '{BlobPath}'")]
    public static partial void BlobNotFound(
        this ILogger logger,
        string blobPath
    );

    /// <summary>
    ///     Logs when compressing snapshot data.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="originalSize">The original data size.</param>
    /// <param name="compressedSize">The compressed data size.</param>
    /// <param name="encoding">The compression encoding.</param>
    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Debug,
        Message = "Compressed snapshot: {OriginalSize} -> {CompressedSize} bytes ({Encoding})")]
    public static partial void CompressedSnapshot(
        this ILogger logger,
        long originalSize,
        long compressedSize,
        string encoding
    );

    /// <summary>
    ///     Logs when the container has been ensured to exist.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="containerName">The container name.</param>
    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Blob container ensured: '{ContainerName}'")]
    public static partial void ContainerEnsured(
        this ILogger logger,
        string containerName
    );

    /// <summary>
    ///     Logs when decompressing snapshot data.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="compressedSize">The compressed data size.</param>
    /// <param name="decompressedSize">The decompressed data size.</param>
    /// <param name="encoding">The compression encoding.</param>
    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Debug,
        Message = "Decompressed snapshot: {CompressedSize} -> {DecompressedSize} bytes ({Encoding})")]
    public static partial void DecompressedSnapshot(
        this ILogger logger,
        long compressedSize,
        long decompressedSize,
        string encoding
    );
}