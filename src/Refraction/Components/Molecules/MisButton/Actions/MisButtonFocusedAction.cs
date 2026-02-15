namespace Mississippi.Refraction.Components.Molecules.MisButtonActions;

/// <summary>
///     Represents a focus interaction action emitted by
///     <see cref="MisButton" />.
/// </summary>
/// <param name="IntentId">The intent identifier from the button view model.</param>
public sealed record MisButtonFocusedAction(string IntentId) : IMisButtonAction;