using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;


namespace Mississippi.Brooks.Runtime.Storage.Cosmos;

/// <summary>
///     Hosted service that ensures the required Cosmos DB database and container exist at startup.
/// </summary>
internal sealed class BrookCosmosContainerInitializer : IHostedService
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BrookCosmosContainerInitializer" /> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="options">The brook storage options.</param>
    [SuppressMessage(
        "Major Code Smell",
        "S1144:Unused private members should be removed",
        Justification = "Constructed via DI reflection")]
    public BrookCosmosContainerInitializer(
        IServiceProvider serviceProvider,
        IOptions<BrookStorageOptions> options
    )
    {
        ServiceProvider = serviceProvider;
        Options = options;
    }

    private IOptions<BrookStorageOptions> Options { get; }

    private IServiceProvider ServiceProvider { get; }

    /// <inheritdoc />
    public async Task StartAsync(
        CancellationToken cancellationToken
    )
    {
        BrookStorageOptions o = Options.Value;
        CosmosClient cosmosClient = ServiceProvider.GetRequiredKeyedService<CosmosClient>(o.CosmosClientServiceKey);
        DatabaseResponse dbResponse = await cosmosClient.CreateDatabaseIfNotExistsAsync(
            o.DatabaseId,
            cancellationToken: cancellationToken);
        Database database = dbResponse.Database;
        try
        {
            Container existingContainer = database.GetContainer(o.ContainerId);
            ContainerResponse containerProperties =
                await existingContainer.ReadContainerAsync(cancellationToken: cancellationToken);
            if (containerProperties.Resource.PartitionKeyPath != "/brookPartitionKey")
            {
                throw new InvalidOperationException(
                    $"Existing Cosmos container '{o.ContainerId}' has partition key path '{containerProperties.Resource.PartitionKeyPath}', but '/brookPartitionKey' is required. Refuse to delete existing container. Please provision a container with the correct partition key.");
            }
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // Container doesn't exist, proceed to create
        }

        await database.CreateContainerIfNotExistsAsync(
            o.ContainerId,
            "/brookPartitionKey",
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public Task StopAsync(
        CancellationToken cancellationToken
    ) =>
        Task.CompletedTask;
}