namespace Mississippi.Refraction.Events;

/// <summary>
///     Event raised when an action is triggered within a component.
/// </summary>
/// <param name="ActionId">The identifier of the action.</param>
/// <param name="Payload">Optional payload data associated with the action.</param>
public sealed record ComponentActionEvent(string ActionId, object? Payload = null);