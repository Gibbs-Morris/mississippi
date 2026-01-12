using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

using Cascade.Contracts.Storage;

using Microsoft.Azure.Cosmos;


namespace Cascade.Server.Services;

/// <summary>
///     Cosmos DB service implementation for connectivity testing.
/// </summary>
internal sealed class CosmosService : ICosmosService
{
    private const string ContainerId = "items";

    private const string DatabaseId = "cascade-web-db";

    /// <summary>
    ///     Initializes a new instance of the <see cref="CosmosService" /> class.
    /// </summary>
    /// <param name="cosmosClient">The Cosmos DB client.</param>
    public CosmosService(
        CosmosClient cosmosClient
    ) =>
        CosmosClient = cosmosClient;

    private CosmosClient CosmosClient { get; }

    /// <inheritdoc />
    public async Task CreateItemAsync(
        CosmosItem item
    )
    {
        Database database = await CosmosClient.CreateDatabaseIfNotExistsAsync(DatabaseId);
        Container container = await database.CreateContainerIfNotExistsAsync(ContainerId, "/id");
        await container.CreateItemAsync(item, new PartitionKey(item.Id));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CosmosItem>> GetItemsAsync()
    {
        Database database = CosmosClient.GetDatabase(DatabaseId);
        Container container = database.GetContainer(ContainerId);
        List<CosmosItem> items = [];
        try
        {
            using FeedIterator<CosmosItem> iterator = container.GetItemQueryIterator<CosmosItem>("SELECT * FROM c");
            while (iterator.HasMoreResults)
            {
                FeedResponse<CosmosItem> response = await iterator.ReadNextAsync();
                items.AddRange(response);
            }
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // Database or container doesn't exist yet
        }

        return items;
    }
}
