namespace Mississippi.Refraction.Components.Molecules.MisButtonActions;

/// <summary>
///     Represents a blur interaction action emitted by
///     <see cref="MisButton" />.
/// </summary>
/// <param name="IntentId">The intent identifier from the button view model.</param>
public sealed record MisButtonBlurredAction(string IntentId) : IMisButtonAction;
