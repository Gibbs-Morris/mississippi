namespace Mississippi.Refraction.Components.Molecules.MisSearchInputActions;

/// <summary>
///     Emitted when the search is submitted (e.g., Enter key pressed).
/// </summary>
/// <param name="IntentId">The intent identifier.</param>
/// <param name="Value">The current search value.</param>
public sealed record MisSearchInputSubmittedAction(
    string IntentId,
    string Value
) : IMisSearchInputAction;
