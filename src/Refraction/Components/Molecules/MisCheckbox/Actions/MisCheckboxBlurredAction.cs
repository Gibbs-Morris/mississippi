namespace Mississippi.Refraction.Components.Molecules.MisCheckboxActions;

/// <summary>
///     Represents a blur interaction action emitted by
///     <see cref="global::Mississippi.Refraction.Components.Molecules.MisCheckbox" />.
/// </summary>
/// <param name="IntentId">The intent identifier from the checkbox view model.</param>
public sealed record MisCheckboxBlurredAction(
    string IntentId
) : IMisCheckboxAction;
