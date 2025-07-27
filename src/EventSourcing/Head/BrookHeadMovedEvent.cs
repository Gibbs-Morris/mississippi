using Mississippi.EventSourcing.Abstractions;


namespace Mississippi.EventSourcing.Head;

public record BrookHeadMovedEvent(BrookPosition NewPosition);