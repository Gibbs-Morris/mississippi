namespace Mississippi.Refraction.Components.Molecules.MisPasswordInputActions;

/// <summary>
///     Emitted when the password input loses focus.
/// </summary>
/// <param name="IntentId">The intent identifier.</param>
public sealed record MisPasswordInputBlurredAction(string IntentId) : IMisPasswordInputAction;