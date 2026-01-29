using Microsoft.Extensions.Logging;


namespace Mississippi.EventSourcing.Snapshots.Blob.Storage;

/// <summary>
///     High-performance logging extensions for <see cref="BlobSnapshotOperations" />.
/// </summary>
internal static partial class BlobSnapshotOperationsLoggerExtensions
{
    /// <summary>
    ///     Logs when a blob was not found.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="blobPath">The blob path.</param>
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Blob not found at path '{BlobPath}'")]
    public static partial void BlobNotFound(
        this ILogger logger,
        string blobPath
    );

    /// <summary>
    ///     Logs when deleting all blobs with a prefix.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="prefix">The blob prefix.</param>
    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Deleting all blobs with prefix '{Prefix}'")]
    public static partial void DeletingAllBlobsWithPrefix(
        this ILogger logger,
        string prefix
    );

    /// <summary>
    ///     Logs when deleting a blob.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="blobPath">The blob path.</param>
    [LoggerMessage(EventId = 3, Level = LogLevel.Debug, Message = "Deleting blob at path '{BlobPath}'")]
    public static partial void DeletingBlob(
        this ILogger logger,
        string blobPath
    );

    /// <summary>
    ///     Logs when writing a blob.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="blobPath">The blob path.</param>
    /// <param name="sizeBytes">The size of the blob in bytes.</param>
    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Debug,
        Message = "Writing blob at path '{BlobPath}' with size {SizeBytes} bytes")]
    public static partial void WritingBlob(
        this ILogger logger,
        string blobPath,
        int sizeBytes
    );
}