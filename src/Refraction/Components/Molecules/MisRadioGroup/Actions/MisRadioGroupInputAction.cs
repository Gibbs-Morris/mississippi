namespace Mississippi.Refraction.Components.Molecules.MisRadioGroupActions;

/// <summary>
///     Represents an input interaction action emitted by
///     <see cref="global::Mississippi.Refraction.Components.Molecules.MisRadioGroup" />.
/// </summary>
/// <param name="IntentId">The intent identifier from the radio group view model.</param>
/// <param name="Value">The selected option value.</param>
public sealed record MisRadioGroupInputAction(string IntentId, string Value) : IMisRadioGroupAction;