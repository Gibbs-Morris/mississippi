namespace Mississippi.Refraction.Components.Molecules.MisSearchInputActions;

/// <summary>
///     Emitted when the search input value changes.
/// </summary>
/// <param name="IntentId">The intent identifier.</param>
/// <param name="Value">The new search value.</param>
public sealed record MisSearchInputChangedAction(string IntentId, string Value) : IMisSearchInputAction;