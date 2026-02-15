namespace Mississippi.Refraction.Components.Molecules.MisCheckboxGroupActions;

/// <summary>
///     Emitted when a checkbox option loses focus.
/// </summary>
/// <param name="IntentId">The intent identifier.</param>
/// <param name="OptionValue">The value of the blurred option.</param>
public sealed record MisCheckboxGroupBlurredAction(
    string IntentId,
    string OptionValue
) : IMisCheckboxGroupAction;
