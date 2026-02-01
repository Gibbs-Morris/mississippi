using Microsoft.Extensions.Logging;


namespace Mississippi.EventSourcing.Snapshots.Blob;

/// <summary>
///     High-performance logging extensions for <see cref="BlobContainerInitializer" />.
/// </summary>
internal static partial class BlobContainerInitializerLoggerExtensions
{
    /// <summary>
    ///     Logs when the blob container was successfully initialized.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        EventId = 101,
        Level = LogLevel.Information,
        Message = "Blob container for snapshot storage initialized successfully")]
    public static partial void BlobContainerInitialized(
        this ILogger logger
    );

    /// <summary>
    ///     Logs when initializing the blob container.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        EventId = 100,
        Level = LogLevel.Information,
        Message = "Initializing blob container for snapshot storage")]
    public static partial void InitializingBlobContainer(
        this ILogger logger
    );
}