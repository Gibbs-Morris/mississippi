using Newtonsoft.Json;


namespace Mississippi.EventSourcing.Cosmos.Storage;

/// <summary>
///     Document model for storing brook cursor position information in Cosmos DB.
/// </summary>
internal class CursorDocument
{
    /// <summary>
    ///     Gets or sets the partition key value for the document. Must match container partition key path
    ///     '/brookPartitionKey'.
    /// </summary>
    [JsonProperty("brookPartitionKey")]
    public string BrookPartitionKey { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the document identifier.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the original position of the brook cursor before any updates.
    /// </summary>
    [JsonProperty("originalPosition")]
    public long? OriginalPosition { get; set; }

    /// <summary>
    ///     Gets or sets the current position of the brook cursor.
    /// </summary>
    [JsonProperty("position")]
    public long Position { get; set; }

    /// <summary>
    ///     Gets or sets the document type.
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;
}