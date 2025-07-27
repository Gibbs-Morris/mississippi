using Mississippi.Core.Abstractions.Streams;
using Newtonsoft.Json;

namespace Mississippi.EventSourcing.Cosmos.Storage;

/// <summary>
/// Document model for storing brook head position information in Cosmos DB.
/// </summary>
internal class HeadDocument
{
    /// <summary>
    /// Gets or sets the document identifier.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the document type.
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current position of the brook head.
    /// </summary>
    [JsonProperty("position")]
    public BrookPosition Position { get; set; }

    /// <summary>
    /// Gets or sets the original position of the brook head before any updates.
    /// </summary>
    [JsonProperty("originalPosition")]
    public BrookPosition? OriginalPosition { get; set; }
}