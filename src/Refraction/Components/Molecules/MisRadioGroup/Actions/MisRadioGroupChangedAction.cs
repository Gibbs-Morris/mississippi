namespace Mississippi.Refraction.Components.Molecules.MisRadioGroupActions;

/// <summary>
///     Represents a change interaction action emitted by
///     <see cref="global::Mississippi.Refraction.Components.Molecules.MisRadioGroup" />.
/// </summary>
/// <param name="IntentId">The intent identifier from the radio group view model.</param>
/// <param name="Value">The selected option value.</param>
public sealed record MisRadioGroupChangedAction(
    string IntentId,
    string Value
) : IMisRadioGroupAction;
