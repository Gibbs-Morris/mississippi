namespace Mississippi.Refraction.Components.Molecules.MisSearchInputActions;

/// <summary>
///     Emitted when the clear button is clicked.
/// </summary>
/// <param name="IntentId">The intent identifier.</param>
public sealed record MisSearchInputClearedAction(
    string IntentId
) : IMisSearchInputAction;
