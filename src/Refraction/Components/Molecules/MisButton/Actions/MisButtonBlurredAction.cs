namespace Mississippi.Refraction.Components.Molecules.MisButton.Actions;

/// <summary>
///     Represents a blur interaction action emitted by
///     <see cref="global::Mississippi.Refraction.Components.Molecules.MisButton.MisButton" />.
/// </summary>
/// <param name="IntentId">The intent identifier from the button view model.</param>
public sealed record MisButtonBlurredAction(string IntentId) : IMisButtonAction;
