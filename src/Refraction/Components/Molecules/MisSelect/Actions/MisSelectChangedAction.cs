namespace Mississippi.Refraction.Components.Molecules.MisSelectActions;

/// <summary>
///     Represents a change interaction action emitted by
///     <see cref="global::Mississippi.Refraction.Components.Molecules.MisSelect" />.
/// </summary>
/// <param name="IntentId">The intent identifier from the select view model.</param>
/// <param name="Value">The committed selected value.</param>
public sealed record MisSelectChangedAction(
    string IntentId,
    string Value
) : IMisSelectAction;