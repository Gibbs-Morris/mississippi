namespace Mississippi.EventSourcing.Cosmos.Storage;

public class EventStorageModel
{
    public string EventId { get; set; } = string.Empty;
    public string? Source { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? DataContentType { get; set; }
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public DateTimeOffset Time { get; set; }
}