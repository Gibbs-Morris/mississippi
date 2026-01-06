using Orleans;


namespace Mississippi.EventSourcing.Brooks.Abstractions.Streaming;

/// <summary>
///     Represents an event that occurs when a brook cursor position has moved to a new position.
///     This event is used to notify subscribers about brook cursor position changes.
/// </summary>
/// <param name="NewPosition">The new position of the brook cursor after the move operation.</param>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.Cursor.BrookCursorMovedEvent")]
public record BrookCursorMovedEvent([property: Id(0)] BrookPosition NewPosition);