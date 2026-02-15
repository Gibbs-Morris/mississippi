namespace Mississippi.Refraction.Components.Molecules.MisSearchInputActions;

/// <summary>
///     Emitted when the search input loses focus.
/// </summary>
/// <param name="IntentId">The intent identifier.</param>
public sealed record MisSearchInputBlurredAction(string IntentId) : IMisSearchInputAction;