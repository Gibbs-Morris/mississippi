namespace Mississippi.Refraction.Components.Molecules.MisPasswordInputActions;

/// <summary>
///     Emitted when the password input value changes.
/// </summary>
/// <param name="IntentId">The intent identifier.</param>
/// <param name="Value">The new password value.</param>
public sealed record MisPasswordInputChangedAction(string IntentId, string Value) : IMisPasswordInputAction;