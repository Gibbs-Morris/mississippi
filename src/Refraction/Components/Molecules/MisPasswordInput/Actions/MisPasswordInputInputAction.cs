namespace Mississippi.Refraction.Components.Molecules.MisPasswordInputActions;

/// <summary>
///     Emitted when the password input receives input.
/// </summary>
/// <param name="IntentId">The intent identifier.</param>
/// <param name="Value">The current password value.</param>
public sealed record MisPasswordInputInputAction(string IntentId, string Value) : IMisPasswordInputAction;