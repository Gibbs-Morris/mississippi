namespace Mississippi.Refraction.Components.Molecules.MisPasswordInputActions;

/// <summary>
///     Emitted when the password visibility toggle is clicked.
/// </summary>
/// <param name="IntentId">The intent identifier.</param>
/// <param name="IsPasswordVisible">The new visibility state.</param>
public sealed record MisPasswordInputToggleVisibilityAction(string IntentId, bool IsPasswordVisible)
    : IMisPasswordInputAction;