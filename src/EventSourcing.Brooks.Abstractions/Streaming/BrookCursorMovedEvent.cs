using Orleans;


namespace Mississippi.EventSourcing.Brooks.Abstractions.Streaming;

/// <summary>
///     Represents an event that occurs when a brook cursor position has moved to a new position.
///     This event is used to notify subscribers about brook cursor position changes.
/// </summary>
/// <param name="BrookKey">The key identifying the brook whose cursor moved.</param>
/// <param name="NewPosition">The new position of the brook cursor after the move operation.</param>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.Brooks.Abstractions.Streaming.BrookCursorMovedEvent")]
public sealed record BrookCursorMovedEvent(
    [property: Id(0)] string BrookKey,
    [property: Id(1)] BrookPosition NewPosition
);