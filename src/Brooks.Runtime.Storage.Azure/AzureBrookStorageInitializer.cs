using System;
using System.Threading;
using System.Threading.Tasks;

using Azure;
using Azure.Storage.Blobs;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace Mississippi.Brooks.Runtime.Storage.Azure;

/// <summary>
///     Validates or provisions the Brooks Azure containers during host startup.
/// </summary>
internal sealed class AzureBrookStorageInitializer : IHostedService
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="AzureBrookStorageInitializer" /> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve the configured keyed blob client.</param>
    /// <param name="options">The Brooks Azure storage options.</param>
    /// <param name="logger">The logger used for sanitized startup diagnostics.</param>
    public AzureBrookStorageInitializer(
        IServiceProvider serviceProvider,
        IOptions<BrookStorageOptions> options,
        ILogger<AzureBrookStorageInitializer> logger
    )
    {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private ILogger<AzureBrookStorageInitializer> Logger { get; }

    private BrookStorageOptions Options { get; }

    private IServiceProvider ServiceProvider { get; }

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
                Options.ContainerName,
                Options.LockContainerName);

            BlobServiceClient blobServiceClient = AzureBlobServiceClientResolver.Resolve(
                ServiceProvider,
                Options.BlobServiceClientServiceKey);

            await EnsureContainerReadyAsync(
                blobServiceClient,
                Options.ContainerName,
                "brooks",
                cancellationToken);
            await EnsureContainerReadyAsync(
                blobServiceClient,
                Options.LockContainerName,
                "locks",
                cancellationToken);

            Logger.CompletedInitialization(Options.InitializationMode, Options.BlobServiceClientServiceKey);
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
    ) =>
        Task.CompletedTask;

    private async Task EnsureContainerReadyAsync(
        BlobServiceClient blobServiceClient,
        string containerName,
        string containerRole,
        CancellationToken cancellationToken
    )
    {
        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        Logger.ValidatingContainer(containerRole, containerName, Options.InitializationMode);

        try
        {
            if (Options.InitializationMode == BrookStorageInitializationMode.ValidateOrCreate)
            {
                await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
            }
            else if (!await containerClient.ExistsAsync(cancellationToken).ConfigureAwait(false))
            {
                throw new InvalidOperationException(
                    $"Azure Brooks storage provider could not find the required {containerRole} container '{containerName}' while running in ValidateOnly mode. Create the container or switch InitializationMode to ValidateOrCreate.");
            }

            await containerClient.GetPropertiesAsync(cancellationToken: cancellationToken);
        }
        catch (RequestFailedException exception)
        {
            throw AzureDiagnosticsRedaction.CreateContainerAccessException(
                containerRole,
                containerName,
                Options.BlobServiceClientServiceKey,
                Options.InitializationMode,
                exception);
        }
    }
}