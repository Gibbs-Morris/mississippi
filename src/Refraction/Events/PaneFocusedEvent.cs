namespace Mississippi.Refraction.Events;

/// <summary>
///     Event raised when a pane receives focus.
/// </summary>
/// <param name="PaneId">The identifier of the focused pane.</param>
public sealed record PaneFocusedEvent(string PaneId);