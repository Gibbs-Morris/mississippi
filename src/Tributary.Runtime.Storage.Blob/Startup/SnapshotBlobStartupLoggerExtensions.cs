using System;

using Microsoft.Extensions.Logging;


namespace Mississippi.Tributary.Runtime.Storage.Blob.Startup;

/// <summary>
///     Logger extensions for Blob snapshot startup validation.
/// </summary>
internal static partial class SnapshotBlobStartupLoggerExtensions
{
    /// <summary>
    ///     Logs that the startup path will create the Blob container when missing.
    /// </summary>
    /// <param name="logger">The logger receiving the event.</param>
    /// <param name="containerName">The configured container name.</param>
    [LoggerMessage(
        EventId = 2410,
        Level = LogLevel.Information,
        Message = "Creating Blob snapshot container '{containerName}' if it does not already exist.")]
    public static partial void CreatingBlobContainer(this ILogger<BlobContainerInitializer> logger, string containerName);

    /// <summary>
    ///     Logs successful serializer validation during startup.
    /// </summary>
    /// <param name="logger">The logger receiving the event.</param>
    /// <param name="configuredFormat">The configured serializer format.</param>
    /// <param name="resolvedFormat">The resolved provider format.</param>
    [LoggerMessage(
        EventId = 2411,
        Level = LogLevel.Information,
        Message = "Validated snapshot payload serializer format '{configuredFormat}' using provider '{resolvedFormat}'.")]
    public static partial void ValidatedPayloadSerializer(
        this ILogger<BlobContainerInitializer> logger,
        string configuredFormat,
        string resolvedFormat);

    /// <summary>
    ///     Logs that startup is validating container existence without creating it.
    /// </summary>
    /// <param name="logger">The logger receiving the event.</param>
    /// <param name="containerName">The configured container name.</param>
    [LoggerMessage(
        EventId = 2412,
        Level = LogLevel.Information,
        Message = "Validating that Blob snapshot container '{containerName}' already exists.")]
    public static partial void ValidatingBlobContainerExists(
        this ILogger<BlobContainerInitializer> logger,
        string containerName);

    /// <summary>
    ///     Logs that startup serializer validation failed.
    /// </summary>
    /// <param name="logger">The logger receiving the event.</param>
    /// <param name="containerName">The configured container name.</param>
    /// <param name="payloadSerializerFormat">The configured serializer format.</param>
    /// <param name="exception">The underlying failure.</param>
    [LoggerMessage(
        EventId = 2413,
        Level = LogLevel.Error,
        Message = "Blob snapshot startup validation failed for container '{containerName}' because payload serializer format '{payloadSerializerFormat}' could not be resolved.")]
    public static partial void BlobStartupSerializerValidationFailed(
        this ILogger<BlobContainerInitializer> logger,
        string containerName,
        string payloadSerializerFormat,
        Exception exception);

    /// <summary>
    ///     Logs that Blob container initialization or validation failed.
    /// </summary>
    /// <param name="logger">The logger receiving the event.</param>
    /// <param name="containerName">The configured container name.</param>
    /// <param name="initializationMode">The configured initialization mode.</param>
    /// <param name="exception">The underlying failure.</param>
    [LoggerMessage(
        EventId = 2414,
        Level = LogLevel.Error,
        Message = "Blob snapshot startup failed for container '{containerName}' using mode '{initializationMode}'.")]
    public static partial void BlobContainerInitializationFailed(
        this ILogger<BlobContainerInitializer> logger,
        string containerName,
        SnapshotBlobContainerInitializationMode initializationMode,
        Exception exception);
}