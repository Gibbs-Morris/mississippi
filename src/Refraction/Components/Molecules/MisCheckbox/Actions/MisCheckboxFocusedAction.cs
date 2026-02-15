namespace Mississippi.Refraction.Components.Molecules.MisCheckboxActions;

/// <summary>
///     Represents a focus interaction action emitted by
///     <see cref="global::Mississippi.Refraction.Components.Molecules.MisCheckbox" />.
/// </summary>
/// <param name="IntentId">The intent identifier from the checkbox view model.</param>
public sealed record MisCheckboxFocusedAction(string IntentId) : IMisCheckboxAction;