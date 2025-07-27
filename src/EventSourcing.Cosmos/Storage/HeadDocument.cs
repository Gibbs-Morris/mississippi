using Mississippi.Core.Abstractions.Streams;
using Newtonsoft.Json;

namespace Mississippi.EventSourcing.Cosmos.Storage;

internal class HeadDocument
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    [JsonProperty("position")]
    public BrookPosition Position { get; set; }

    [JsonProperty("originalPosition")]
    public BrookPosition? OriginalPosition { get; set; }
}