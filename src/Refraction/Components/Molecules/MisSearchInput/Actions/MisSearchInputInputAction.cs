namespace Mississippi.Refraction.Components.Molecules.MisSearchInputActions;

/// <summary>
///     Emitted when the search input receives input.
/// </summary>
/// <param name="IntentId">The intent identifier.</param>
/// <param name="Value">The current search value.</param>
public sealed record MisSearchInputInputAction(
    string IntentId,
    string Value
) : IMisSearchInputAction;
