using System.Collections.Generic;
using System.Threading.Tasks;

using Cascade.Contracts;


namespace Cascade.Server.Services;

/// <summary>
///     Service for Cosmos DB operations.
/// </summary>
internal interface ICosmosService
{
    /// <summary>
    ///     Creates a new item in the container.
    /// </summary>
    /// <param name="item">The item to create.</param>
    /// <returns>A task representing the async operation.</returns>
    Task CreateItemAsync(
        CosmosItem item
    );

    /// <summary>
    ///     Gets all items from the container.
    /// </summary>
    /// <returns>A list of items.</returns>
    Task<IReadOnlyList<CosmosItem>> GetItemsAsync();
}