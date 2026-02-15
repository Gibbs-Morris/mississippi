namespace Mississippi.Refraction.Components.Molecules.MisTextInputActions;

/// <summary>
///     Represents an input interaction action emitted by
///     <see cref="global::Mississippi.Refraction.Components.Molecules.MisTextInput" />.
/// </summary>
/// <param name="IntentId">The intent identifier from the text input view model.</param>
/// <param name="Value">The latest input value.</param>
public sealed record MisTextInputInputAction(string IntentId, string Value) : IMisTextInputAction;