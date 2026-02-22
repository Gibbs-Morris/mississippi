namespace Mississippi.Refraction.Events;

/// <summary>
///     Event raised when a confirmation dialog is resolved.
/// </summary>
/// <param name="Confirmed">Whether the user confirmed (true) or cancelled (false).</param>
public sealed record ConfirmationResolvedEvent(bool Confirmed);