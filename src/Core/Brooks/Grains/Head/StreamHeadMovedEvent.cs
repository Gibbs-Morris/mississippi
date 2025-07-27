using Mississippi.Core.Abstractions.Streams;

namespace Mississippi.Core.Brooks.Grains.Head;

public record StreamHeadMovedEvent(BrookPosition NewPosition);