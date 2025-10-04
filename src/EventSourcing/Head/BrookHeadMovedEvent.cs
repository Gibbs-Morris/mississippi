using Mississippi.EventSourcing.Abstractions;


namespace Mississippi.EventSourcing.Head;

/// <summary>
///     Represents an event that occurs when a brook head position has moved to a new position.
///     This event is used to notify subscribers about brook head position changes.
/// </summary>
/// <param name="NewPosition">The new position of the brook head after the move operation.</param>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.Head.BrookHeadMovedEvent")]
public record BrookHeadMovedEvent([property: Id(0)] BrookPosition NewPosition);
