using Newtonsoft.Json;


namespace Cascade.Contracts.Storage;

/// <summary>
///     Represents an item stored in Cosmos DB.
/// </summary>
public sealed record CosmosItem
{
    /// <summary>
    ///     Gets the data content.
    /// </summary>
    [JsonProperty("data")]
    public required string Data { get; init; }

    /// <summary>
    ///     Gets the unique identifier.
    /// </summary>
    [JsonProperty("id")]
    public required string Id { get; init; }
}