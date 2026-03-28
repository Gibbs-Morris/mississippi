using Microsoft.Extensions.Logging;


namespace Mississippi.Brooks.Runtime.Storage.Azure;

/// <summary>
///     High-performance logging extensions for Brooks Azure startup validation.
/// </summary>
internal static partial class AzureBrookStorageInitializerLoggerExtensions
{
    /// <summary>
    ///     Logs the start of Brooks Azure storage initialization.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="initializationMode">The startup initialization mode.</param>
    /// <param name="blobServiceClientServiceKey">The keyed client registration used for Azure access.</param>
    /// <param name="containerName">The Brooks event container name.</param>
    /// <param name="lockContainerName">The Brooks lock container name.</param>
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Starting Brooks Azure storage initialization in mode {InitializationMode} using BlobServiceClient service key '{BlobServiceClientServiceKey}' for containers '{ContainerName}' and '{LockContainerName}'")]
    public static partial void StartingInitialization(
        this ILogger logger,
        BrookStorageInitializationMode initializationMode,
        string blobServiceClientServiceKey,
        string containerName,
        string lockContainerName
    );

    /// <summary>
    ///     Logs per-container validation activity during startup.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="containerRole">The logical role of the container being validated.</param>
    /// <param name="containerName">The configured container name.</param>
    /// <param name="initializationMode">The startup initialization mode.</param>
    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "Validating Brooks Azure {ContainerRole} container '{ContainerName}' in mode {InitializationMode}")]
    public static partial void ValidatingContainer(
        this ILogger logger,
        string containerRole,
        string containerName,
        BrookStorageInitializationMode initializationMode
    );

    /// <summary>
    ///     Logs successful completion of Brooks Azure startup validation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="initializationMode">The startup initialization mode.</param>
    /// <param name="blobServiceClientServiceKey">The keyed client registration used for Azure access.</param>
    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Information,
        Message = "Completed Brooks Azure storage initialization in mode {InitializationMode} using BlobServiceClient service key '{BlobServiceClientServiceKey}'")]
    public static partial void CompletedInitialization(
        this ILogger logger,
        BrookStorageInitializationMode initializationMode,
        string blobServiceClientServiceKey
    );

    /// <summary>
    ///     Logs a sanitized Brooks Azure startup validation failure.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="reason">The sanitized failure reason.</param>
    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Error,
        Message = "Brooks Azure storage initialization failed: {Reason}")]
    public static partial void InitializationFailed(
        this ILogger logger,
        string reason
    );
}