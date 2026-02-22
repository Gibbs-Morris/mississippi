namespace Mississippi.Refraction.Events;

/// <summary>
///     Event raised when a command orbit action is selected.
/// </summary>
/// <param name="ActionId">The identifier of the selected action.</param>
/// <param name="IsCritical">Whether this is a critical/destructive action.</param>
public sealed record CommandSelectedEvent(string ActionId, bool IsCritical = false);