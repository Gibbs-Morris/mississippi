namespace Mississippi.Refraction.Events;

/// <summary>
///     Event raised when a pane is closed.
/// </summary>
/// <param name="Reason">The reason for closure (e.g., "keyboard-escape", "click-outside", "action-complete").</param>
public sealed record PaneClosedEvent(string Reason);