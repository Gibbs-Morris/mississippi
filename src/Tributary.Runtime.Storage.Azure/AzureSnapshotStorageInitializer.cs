using System;
using System.Threading;
using System.Threading.Tasks;

using Azure;
using Azure.Storage.Blobs;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace Mississippi.Tributary.Runtime.Storage.Azure
{
    /// <summary>
    ///     Validates or provisions the Tributary Azure snapshot container during host startup.
    /// </summary>
    internal sealed class AzureSnapshotStorageInitializer(
        IServiceProvider serviceProvider,
        IOptions<SnapshotStorageOptions> options,
        ILogger<AzureSnapshotStorageInitializer> logger) : IHostedService
    {
        private ILogger<AzureSnapshotStorageInitializer> Logger { get; } = logger ?? throw new ArgumentNullException(nameof(logger));

        private SnapshotStorageOptions Options { get; } = options?.Value ?? throw new ArgumentNullException(nameof(options));

        private IServiceProvider ServiceProvider { get; } = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        /// <inheritdoc />
        public async Task StartAsync(
            CancellationToken cancellationToken
        )
        {
            try
            {
                Logger.StartingInitialization(
                    Options.InitializationMode,
                    Options.BlobServiceClientServiceKey,
                    Options.ContainerName);

                BlobServiceClient blobServiceClient = AzureBlobServiceClientResolver.Resolve(
                    ServiceProvider,
                    Options.BlobServiceClientServiceKey);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(Options.ContainerName);

                Logger.ValidatingContainer(Options.ContainerName, Options.InitializationMode);

                if (Options.InitializationMode == SnapshotStorageInitializationMode.ValidateOrCreate)
                {
                    _ = await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                else if (!await containerClient.ExistsAsync(cancellationToken).ConfigureAwait(false))
                {
                    throw new InvalidOperationException(
                        $"Azure Tributary snapshot storage provider could not find the required snapshot container '{Options.ContainerName}' while running in ValidateOnly mode. Create the container or switch InitializationMode to ValidateOrCreate.");
                }

                _ = await containerClient.GetPropertiesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

                Logger.CompletedInitialization(Options.InitializationMode, Options.BlobServiceClientServiceKey);
            }
            catch (RequestFailedException exception)
            {
                InvalidOperationException wrapped = AzureDiagnosticsRedaction.CreateContainerAccessException(
                    Options.ContainerName,
                    Options.BlobServiceClientServiceKey,
                    Options.InitializationMode,
                    exception);
                Logger.InitializationFailed(wrapped.Message);
                throw wrapped;
            }
            catch (InvalidOperationException exception)
            {
                Logger.InitializationFailed(exception.Message);
                throw;
            }
        }

        /// <inheritdoc />
        public Task StopAsync(
            CancellationToken cancellationToken
        )
        {
            return Task.CompletedTask;
        }
    }
}
