using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.Brooks.Serialization.Abstractions;


namespace Mississippi.Tributary.Runtime.Storage.Blob.Startup;

/// <summary>
///     Performs Blob container and serializer startup validation for the snapshot provider.
/// </summary>
internal sealed class BlobContainerInitializer : IHostedService
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BlobContainerInitializer" /> class.
    /// </summary>
    /// <param name="blobContainerInitializerOperations">The Blob container operations used during startup.</param>
    /// <param name="snapshotPayloadSerializerResolver">The configured serializer resolver.</param>
    /// <param name="options">The Blob snapshot storage options.</param>
    /// <param name="logger">The logger for startup diagnostics.</param>
    public BlobContainerInitializer(
        IBlobContainerInitializerOperations blobContainerInitializerOperations,
        SnapshotPayloadSerializerResolver snapshotPayloadSerializerResolver,
        IOptions<SnapshotBlobStorageOptions> options,
        ILogger<BlobContainerInitializer> logger
    )
    {
        BlobContainerInitializerOperations = blobContainerInitializerOperations ??
                                             throw new ArgumentNullException(
                                                 nameof(blobContainerInitializerOperations));
        SnapshotPayloadSerializerResolver = snapshotPayloadSerializerResolver ??
                                            throw new ArgumentNullException(nameof(snapshotPayloadSerializerResolver));
        Options = options ?? throw new ArgumentNullException(nameof(options));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private IBlobContainerInitializerOperations BlobContainerInitializerOperations { get; }

    private ILogger<BlobContainerInitializer> Logger { get; }

    private IOptions<SnapshotBlobStorageOptions> Options { get; }

    private SnapshotPayloadSerializerResolver SnapshotPayloadSerializerResolver { get; }

    /// <inheritdoc />
    public async Task StartAsync(
        CancellationToken cancellationToken
    )
    {
        SnapshotBlobStorageOptions options = Options.Value;
        ISerializationProvider serializer;
        try
        {
            serializer = SnapshotPayloadSerializerResolver.ResolveConfiguredSerializer();
        }
        catch (InvalidOperationException exception)
        {
            Logger.BlobStartupSerializerValidationFailed(
                options.ContainerName,
                options.PayloadSerializerFormat,
                exception);
            throw new InvalidOperationException(
                $"Blob snapshot storage startup validation failed for container '{options.ContainerName}' because payload serializer format '{options.PayloadSerializerFormat}' is not usable. {exception.Message}",
                exception);
        }

        Logger.ValidatedPayloadSerializer(options.PayloadSerializerFormat, serializer.Format);
        switch (options.ContainerInitializationMode)
        {
            case SnapshotBlobContainerInitializationMode.CreateIfMissing:
                Logger.CreatingBlobContainer(options.ContainerName);
                try
                {
                    await BlobContainerInitializerOperations.CreateIfNotExistsAsync(cancellationToken);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    Logger.BlobContainerInitializationFailed(
                        options.ContainerName,
                        options.ContainerInitializationMode,
                        exception);
                    throw new InvalidOperationException(
                        $"Blob snapshot storage startup failed while initializing container '{options.ContainerName}' using mode '{options.ContainerInitializationMode}'. Verify the BlobServiceClient registration, storage account credentials, and that the account permits creating containers.",
                        exception);
                }

                break;
            case SnapshotBlobContainerInitializationMode.ValidateExists:
                Logger.ValidatingBlobContainerExists(options.ContainerName);
                bool containerExists;
                try
                {
                    containerExists = await BlobContainerInitializerOperations.ExistsAsync(cancellationToken);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    Logger.BlobContainerInitializationFailed(
                        options.ContainerName,
                        options.ContainerInitializationMode,
                        exception);
                    throw new InvalidOperationException(
                        $"Blob snapshot storage startup failed while validating container '{options.ContainerName}' using mode '{options.ContainerInitializationMode}'. Verify the BlobServiceClient registration, storage account credentials, and that the configured container name is correct.",
                        exception);
                }

                if (!containerExists)
                {
                    throw new InvalidOperationException(
                        $"Azure Blob snapshot container '{options.ContainerName}' does not exist while ContainerInitializationMode is '{options.ContainerInitializationMode}'. Set ContainerInitializationMode to CreateIfMissing or provision the container before startup.");
                }

                break;
            default:
                throw new InvalidOperationException(
                    $"Container initialization mode '{options.ContainerInitializationMode}' is not supported.");
        }
    }

    /// <inheritdoc />
    public Task StopAsync(
        CancellationToken cancellationToken
    ) =>
        Task.CompletedTask;
}