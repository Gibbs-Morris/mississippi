namespace Mississippi.Refraction.Components.Molecules.MisTextInputActions;

/// <summary>
///     Represents a change interaction action emitted by
///     <see cref="global::Mississippi.Refraction.Components.Molecules.MisTextInput" />.
/// </summary>
/// <param name="IntentId">The intent identifier from the text input view model.</param>
/// <param name="Value">The committed input value.</param>
public sealed record MisTextInputChangedAction(
    string IntentId,
    string Value
) : IMisTextInputAction;