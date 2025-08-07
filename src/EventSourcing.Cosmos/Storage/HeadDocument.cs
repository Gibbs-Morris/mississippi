using Mississippi.EventSourcing.Abstractions;

using Newtonsoft.Json;


namespace Mississippi.EventSourcing.Cosmos.Storage;

/// <summary>
///     Document model for storing brook head position information in Cosmos DB.
/// </summary>
internal class HeadDocument
{
    /// <summary>
    ///     Gets or sets the document identifier.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the document type.
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    ///     Partition key value for the document. Must match container partition key path '/brookPartitionKey'.
    /// </summary>
    [JsonProperty("brookPartitionKey")]
    public string BrookPartitionKey { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the current position of the brook head.
    /// </summary>
    [JsonProperty("position")]
    public long Position { get; set; }

    /// <summary>
    ///     Gets or sets the original position of the brook head before any updates.
    /// </summary>
    [JsonProperty("originalPosition")]
    public long? OriginalPosition { get; set; }
}