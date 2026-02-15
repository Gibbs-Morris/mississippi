namespace Mississippi.Refraction.Components.Molecules.MisPasswordInputActions;

/// <summary>
///     Emitted when the password input receives focus.
/// </summary>
/// <param name="IntentId">The intent identifier.</param>
public sealed record MisPasswordInputFocusedAction(
    string IntentId
) : IMisPasswordInputAction;
