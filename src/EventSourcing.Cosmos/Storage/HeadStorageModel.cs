using Mississippi.Core.Abstractions.Streams;

namespace Mississippi.EventSourcing.Cosmos.Storage;

public class HeadStorageModel
{
    public BrookPosition Position { get; set; }
    public BrookPosition? OriginalPosition { get; set; }
}