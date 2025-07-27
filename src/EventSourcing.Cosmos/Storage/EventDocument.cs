using Newtonsoft.Json;

namespace Mississippi.EventSourcing.Cosmos.Storage;

internal class EventDocument
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    [JsonProperty("position")]
    public long Position { get; set; }

    [JsonProperty("eventId")]
    public string EventId { get; set; } = string.Empty;

    [JsonProperty("source")]
    public string? Source { get; set; }

    [JsonProperty("eventType")]
    public string EventType { get; set; } = string.Empty;

    [JsonProperty("dataContentType")]
    public string? DataContentType { get; set; }

    [JsonProperty("data")]
    public byte[] Data { get; set; } = Array.Empty<byte>();

    [JsonProperty("time")]
    public DateTimeOffset Time { get; set; }
}