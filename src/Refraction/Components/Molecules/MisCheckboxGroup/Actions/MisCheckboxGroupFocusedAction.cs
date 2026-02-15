namespace Mississippi.Refraction.Components.Molecules.MisCheckboxGroupActions;

/// <summary>
///     Emitted when a checkbox option receives focus.
/// </summary>
/// <param name="IntentId">The intent identifier.</param>
/// <param name="OptionValue">The value of the focused option.</param>
public sealed record MisCheckboxGroupFocusedAction(string IntentId, string OptionValue) : IMisCheckboxGroupAction;