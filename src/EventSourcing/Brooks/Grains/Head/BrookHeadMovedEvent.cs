using Mississippi.EventSourcing.Abstractions.Brooks;


namespace Mississippi.EventSourcing.Brooks.Grains.Head;

public record BrookHeadMovedEvent(BrookPosition NewPosition);