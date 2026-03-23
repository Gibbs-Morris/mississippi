using Microsoft.Extensions.Logging;


namespace Mississippi.Tributary.Runtime.Storage.Blob;

/// <summary>
///     Logger extensions for the Blob snapshot storage provider shell.
/// </summary>
internal static partial class SnapshotBlobStorageProviderLoggerExtensions
{
    /// <summary>
    ///     Logs that a Blob snapshot operation has not yet been implemented.
    /// </summary>
    /// <param name="logger">The logger receiving the event.</param>
    /// <param name="operationName">The storage operation name.</param>
    [LoggerMessage(
        EventId = 2400,
        Level = LogLevel.Warning,
        Message = "Snapshot Blob operation '{operationName}' is not implemented in increment 1.")]
    public static partial void SnapshotOperationNotImplemented(
        this ILogger<SnapshotBlobStorageProvider> logger,
        string operationName);
}