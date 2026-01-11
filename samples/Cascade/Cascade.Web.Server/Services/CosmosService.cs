using System.Collections.Generic;
using System.Threading.Tasks;

using Cascade.Web.Contracts;

using Microsoft.Azure.Cosmos;


namespace Cascade.Web.Server.Services;

/// <summary>
///     Cosmos DB service implementation for connectivity testing.
/// </summary>
internal sealed class CosmosService : ICosmosService
{
    private const string DatabaseId = "cascade-web-db";
    private const string ContainerId = "items";

    private CosmosClient CosmosClient { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="CosmosService" /> class.
    /// </summary>
    /// <param name="cosmosClient">The Cosmos DB client.</param>
    public CosmosService(
        CosmosClient cosmosClient
    ) =>
        CosmosClient = cosmosClient;

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
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Database or container doesn't exist yet
        }

        return items;
    }
}
