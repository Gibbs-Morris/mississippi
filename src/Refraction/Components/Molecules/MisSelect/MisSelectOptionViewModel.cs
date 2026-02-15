namespace Mississippi.Refraction.Components.Molecules;

/// <summary>
///     Represents a selectable option rendered by <see cref="MisSelect" />.
/// </summary>
/// <param name="Value">The option value.</param>
/// <param name="Label">The visible option label.</param>
/// <param name="IsDisabled">A value indicating whether the option is disabled.</param>
public sealed record MisSelectOptionViewModel(string Value, string Label, bool IsDisabled = false);