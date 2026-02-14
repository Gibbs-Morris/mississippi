namespace Mississippi.Refraction.Components.Molecules.MisButton.Actions;

/// <summary>
///     Represents a focus interaction action emitted by
///     <see cref="global::Mississippi.Refraction.Components.Molecules.MisButton.MisButton" />.
/// </summary>
/// <param name="IntentId">The intent identifier from the button view model.</param>
public sealed record MisButtonFocusedAction(string IntentId) : IMisButtonAction;
