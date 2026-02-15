namespace Mississippi.Refraction.Components.Molecules;

/// <summary>
///     Represents a selectable checkbox option rendered by <see cref="MisCheckboxGroup" />.
/// </summary>
/// <param name="Value">The option value.</param>
/// <param name="Label">The visible option label.</param>
/// <param name="IsDisabled">A value indicating whether the option is disabled.</param>
public sealed record MisCheckboxOptionViewModel(
    string Value,
    string Label,
    bool IsDisabled = false
);
