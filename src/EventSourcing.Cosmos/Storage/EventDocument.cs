using Newtonsoft.Json;


namespace Mississippi.EventSourcing.Cosmos.Storage;

/// <summary>
///     Document model for storing events in Cosmos DB.
/// </summary>
internal class EventDocument
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
    ///     Gets or sets the partition key value for the document. Must match container partition key path
    ///     '/brookPartitionKey'.
    /// </summary>
    [JsonProperty("brookPartitionKey")]
    public string BrookPartitionKey { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the position of the event in the brook.
    /// </summary>
    [JsonProperty("position")]
    public long Position { get; set; }

    /// <summary>
    ///     Gets or sets the unique identifier of the event.
    /// </summary>
    [JsonProperty("eventId")]
    public string EventId { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the source of the event.
    /// </summary>
    [JsonProperty("source")]
    public string? Source { get; set; }

    /// <summary>
    ///     Gets or sets the type of the event.
    /// </summary>
    [JsonProperty("eventType")]
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the content type of the event data.
    /// </summary>
    [JsonProperty("dataContentType")]
    public string? DataContentType { get; set; }

    /// <summary>
    ///     Gets or sets the event data as a byte array.
    /// </summary>
    [JsonProperty("data")]
    public byte[] Data { get; set; } = Array.Empty<byte>();

    /// <summary>
    ///     Gets or sets the timestamp when the event occurred.
    /// </summary>
    [JsonProperty("time")]
    public DateTimeOffset Time { get; set; }
}