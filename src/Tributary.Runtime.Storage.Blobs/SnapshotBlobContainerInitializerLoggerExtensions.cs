using Microsoft.Extensions.Logging;


namespace Mississippi.Tributary.Runtime.Storage.Blobs;

/// <summary>
///     High-performance logging extensions for <see cref="SnapshotBlobContainerInitializer" />.
/// </summary>
internal static partial class SnapshotBlobContainerInitializerLoggerExtensions
{
    /// <summary>
    ///     Logs when the snapshot container check begins.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Ensuring the Blob snapshot container exists.")]
    public static partial void EnsuringSnapshotContainer(
        this ILogger logger
    );

    /// <summary>
    ///     Logs when the snapshot container is ready.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "The Blob snapshot container is ready.")]
    public static partial void SnapshotContainerReady(
        this ILogger logger
    );
}