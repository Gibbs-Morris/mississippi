namespace Mississippi.Refraction.Components.Molecules.MisTextInputActions;

/// <summary>
///     Represents a blur interaction action emitted by
///     <see cref="global::Mississippi.Refraction.Components.Molecules.MisTextInput" />.
/// </summary>
/// <param name="IntentId">The intent identifier from the text input view model.</param>
public sealed record MisTextInputBlurredAction(string IntentId) : IMisTextInputAction;