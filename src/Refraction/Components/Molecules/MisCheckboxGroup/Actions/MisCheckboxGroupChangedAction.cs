using System.Collections.Generic;


namespace Mississippi.Refraction.Components.Molecules.MisCheckboxGroupActions;

/// <summary>
///     Emitted when a checkbox option is toggled.
/// </summary>
/// <param name="IntentId">The intent identifier.</param>
/// <param name="Value">The option value that was toggled.</param>
/// <param name="IsChecked">Whether the option is now checked.</param>
/// <param name="Values">The current set of selected values.</param>
public sealed record MisCheckboxGroupChangedAction(
    string IntentId,
    string Value,
    bool IsChecked,
    IReadOnlySet<string> Values
) : IMisCheckboxGroupAction;