using System;

using Microsoft.Extensions.Logging;


namespace Mississippi.Tributary.Runtime.Storage.Blobs.Storage;

/// <summary>
///     High-performance logging extensions for <see cref="SnapshotBlobRepository" />.
/// </summary>
internal static partial class SnapshotBlobRepositoryLoggerExtensions
{
    /// <summary>
    ///     Logs when all snapshots for a prefix are being deleted.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="prefix">The Blob prefix.</param>
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Deleting Blob snapshots under prefix '{Prefix}'.")]
    public static partial void DeletingAllSnapshots(
        this ILogger logger,
        string prefix
    );

    /// <summary>
    ///     Logs when a Blob snapshot document is invalid.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="blobName">The Blob name.</param>
    /// <param name="reason">The failure reason.</param>
    /// <param name="exception">The exception.</param>
    [LoggerMessage(EventId = 6, Level = LogLevel.Error, Message = "Blob snapshot '{BlobName}' is invalid: {Reason}")]
    public static partial void InvalidSnapshotDocument(
        this ILogger logger,
        string blobName,
        string reason,
        Exception exception
    );

    /// <summary>
    ///     Logs when a prune operation begins.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="prefix">The Blob prefix.</param>
    /// <param name="moduliCount">The number of moduli.</param>
    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "Pruning Blob snapshots under prefix '{Prefix}' with {ModuliCount} moduli.")]
    public static partial void PruningSnapshots(
        this ILogger logger,
        string prefix,
        int moduliCount
    );

    /// <summary>
    ///     Logs when a Blob snapshot was deleted.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="blobName">The Blob name.</param>
    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "Deleted Blob snapshot '{BlobName}'.")]
    public static partial void SnapshotDeleted(
        this ILogger logger,
        string blobName
    );

    /// <summary>
    ///     Logs when a Blob snapshot is missing.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="blobName">The Blob name.</param>
    [LoggerMessage(EventId = 5, Level = LogLevel.Debug, Message = "Blob snapshot '{BlobName}' was not found.")]
    public static partial void SnapshotNotFound(
        this ILogger logger,
        string blobName
    );

    /// <summary>
    ///     Logs when a Blob snapshot read succeeds.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="blobName">The Blob name.</param>
    [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = "Read Blob snapshot '{BlobName}'.")]
    public static partial void SnapshotRead(
        this ILogger logger,
        string blobName
    );

    /// <summary>
    ///     Logs when a Blob snapshot upload succeeds.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="blobName">The Blob name.</param>
    [LoggerMessage(EventId = 7, Level = LogLevel.Debug, Message = "Uploaded Blob snapshot '{BlobName}'.")]
    public static partial void SnapshotUploaded(
        this ILogger logger,
        string blobName
    );
}