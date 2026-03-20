using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;


namespace Mississippi.Tributary.Runtime.Storage.Cosmos;

/// <summary>
///     Hosted service that ensures the required Cosmos DB database and container exist at startup.
/// </summary>
internal sealed class SnapshotCosmosContainerInitializer : IHostedService
{
    private const string PartitionKeyPath = "/snapshotPartitionKey";

    /// <summary>
    ///     Initializes a new instance of the <see cref="SnapshotCosmosContainerInitializer" /> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="options">The snapshot storage options.</param>
    [SuppressMessage("Major Code Smell", "S1144", Justification = "Used by DI")]
    public SnapshotCosmosContainerInitializer(
        IServiceProvider serviceProvider,
        IOptions<SnapshotStorageOptions> options
    )
    {
        ServiceProvider = serviceProvider;
        Options = options;
    }

    private IOptions<SnapshotStorageOptions> Options { get; }

    private IServiceProvider ServiceProvider { get; }

    /// <inheritdoc />
    public async Task StartAsync(
        CancellationToken cancellationToken
    )
    {
        SnapshotStorageOptions o = Options.Value;
        CosmosClient cosmosClient = ServiceProvider.GetRequiredKeyedService<CosmosClient>(o.CosmosClientServiceKey);
        DatabaseResponse db = await cosmosClient.CreateDatabaseIfNotExistsAsync(
            o.DatabaseId,
            cancellationToken: cancellationToken);
        Database database = db.Database;
        try
        {
            ContainerResponse container = await database.GetContainer(o.ContainerId)
                .ReadContainerAsync(cancellationToken: cancellationToken);
            if (!string.Equals(container.Resource.PartitionKeyPath, PartitionKeyPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Existing Cosmos container '{o.ContainerId}' has partition key path '{container.Resource.PartitionKeyPath}', but '{PartitionKeyPath}' is required.");
            }
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // Container will be created below
        }

        await database.CreateContainerIfNotExistsAsync(
            o.ContainerId,
            PartitionKeyPath,
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public Task StopAsync(
        CancellationToken cancellationToken
    ) =>
        Task.CompletedTask;
}