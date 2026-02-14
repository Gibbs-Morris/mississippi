namespace Mississippi.Refraction.Components.Molecules.MisSelectActions;

/// <summary>
///     Represents a focus interaction action emitted by
///     <see cref="global::Mississippi.Refraction.Components.Molecules.MisSelect" />.
/// </summary>
/// <param name="IntentId">The intent identifier from the select view model.</param>
public sealed record MisSelectFocusedAction(
    string IntentId
) : IMisSelectAction;