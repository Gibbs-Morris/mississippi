using Microsoft.Extensions.Logging;


namespace Mississippi.Tributary.Runtime.Storage.Azure;

/// <summary>
///     High-performance logging extensions for Tributary Azure startup initialization.
/// </summary>
internal static partial class AzureSnapshotStorageInitializerLoggerExtensions
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Starting Tributary Azure snapshot storage initialization in {InitializationMode} mode using service key '{BlobServiceClientServiceKey}' and container '{ContainerName}'")]
    public static partial void StartingInitialization(
        this ILogger logger,
        SnapshotStorageInitializationMode initializationMode,
        string blobServiceClientServiceKey,
        string containerName
    );

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "Validating Tributary Azure snapshot container '{ContainerName}' in {InitializationMode} mode")]
    public static partial void ValidatingContainer(
        this ILogger logger,
        string containerName,
        SnapshotStorageInitializationMode initializationMode
    );

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Information,
        Message = "Completed Tributary Azure snapshot storage initialization in {InitializationMode} mode using service key '{BlobServiceClientServiceKey}'")]
    public static partial void CompletedInitialization(
        this ILogger logger,
        SnapshotStorageInitializationMode initializationMode,
        string blobServiceClientServiceKey
    );

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Error,
        Message = "Tributary Azure snapshot storage initialization failed: {Message}")]
    public static partial void InitializationFailed(
        this ILogger logger,
        string message
    );
}
