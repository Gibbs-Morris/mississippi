namespace Mississippi.EventSourcing.Cosmos.Storage;

/// <summary>
/// Storage model for event data.
/// </summary>
internal class EventStorageModel
{
    /// <summary>
    /// Gets or sets the unique identifier of the event.
    /// </summary>
    public string EventId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source of the event.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets the type of the event.
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content type of the event data.
    /// </summary>
    public string? DataContentType { get; set; }

    /// <summary>
    /// Gets or sets the event data as a byte array.
    /// </summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the timestamp when the event occurred.
    /// </summary>
    public DateTimeOffset Time { get; set; }
}