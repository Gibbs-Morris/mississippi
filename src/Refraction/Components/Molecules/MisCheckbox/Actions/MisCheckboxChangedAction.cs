namespace Mississippi.Refraction.Components.Molecules.MisCheckboxActions;

/// <summary>
///     Represents a change interaction action emitted by
///     <see cref="global::Mississippi.Refraction.Components.Molecules.MisCheckbox" />.
/// </summary>
/// <param name="IntentId">The intent identifier from the checkbox view model.</param>
/// <param name="IsChecked">A value indicating whether the checkbox is checked.</param>
public sealed record MisCheckboxChangedAction(string IntentId, bool IsChecked) : IMisCheckboxAction;