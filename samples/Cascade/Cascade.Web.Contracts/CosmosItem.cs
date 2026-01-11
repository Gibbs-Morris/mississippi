using System.Text.Json.Serialization;


namespace Cascade.Web.Contracts;

/// <summary>
///     Represents an item stored in Cosmos DB.
/// </summary>
public sealed record CosmosItem
{
    /// <summary>
    ///     Gets the unique identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    ///     Gets the data content.
    /// </summary>
    public required string Data { get; init; }
}
