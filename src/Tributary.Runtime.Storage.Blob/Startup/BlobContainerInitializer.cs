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
        BlobContainerInitializerOperations = blobContainerInitializerOperations
            ?? throw new ArgumentNullException(nameof(blobContainerInitializerOperations));
        SnapshotPayloadSerializerResolver = snapshotPayloadSerializerResolver
            ?? throw new ArgumentNullException(nameof(snapshotPayloadSerializerResolver));
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
        ISerializationProvider serializer = SnapshotPayloadSerializerResolver.ResolveConfiguredSerializer();
        Logger.ValidatedPayloadSerializer(options.PayloadSerializerFormat, serializer.Format);

        switch (options.ContainerInitializationMode)
        {
            case SnapshotBlobContainerInitializationMode.CreateIfMissing:
                Logger.CreatingBlobContainer(options.ContainerName);
                await BlobContainerInitializerOperations.CreateIfNotExistsAsync(cancellationToken);
                break;

            case SnapshotBlobContainerInitializationMode.ValidateExists:
                Logger.ValidatingBlobContainerExists(options.ContainerName);
                if (!await BlobContainerInitializerOperations.ExistsAsync(cancellationToken))
                {
                    throw new InvalidOperationException(
                        $"Azure Blob container '{options.ContainerName}' does not exist. Set ContainerInitializationMode to CreateIfMissing or provision the container before startup.");
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