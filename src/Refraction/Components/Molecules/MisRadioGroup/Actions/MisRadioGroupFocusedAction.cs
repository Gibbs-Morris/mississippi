namespace Mississippi.Refraction.Components.Molecules.MisRadioGroupActions;

/// <summary>
///     Represents a focus interaction action emitted by
///     <see cref="global::Mississippi.Refraction.Components.Molecules.MisRadioGroup" />.
/// </summary>
/// <param name="IntentId">The intent identifier from the radio group view model.</param>
/// <param name="OptionValue">The focused option value.</param>
public sealed record MisRadioGroupFocusedAction(string IntentId, string OptionValue) : IMisRadioGroupAction;