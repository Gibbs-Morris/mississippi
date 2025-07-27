using Mississippi.Core.Abstractions.Brooks;


namespace Mississippi.Core.Brooks.Grains.Head;

public record BrookHeadMovedEvent(BrookPosition NewPosition);