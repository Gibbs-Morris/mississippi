namespace Mississippi.Refraction.Components.Molecules.MisRadioGroupActions;

/// <summary>
///     Represents a blur interaction action emitted by
///     <see cref="global::Mississippi.Refraction.Components.Molecules.MisRadioGroup" />.
/// </summary>
/// <param name="IntentId">The intent identifier from the radio group view model.</param>
/// <param name="OptionValue">The blurred option value.</param>
public sealed record MisRadioGroupBlurredAction(string IntentId, string OptionValue) : IMisRadioGroupAction;