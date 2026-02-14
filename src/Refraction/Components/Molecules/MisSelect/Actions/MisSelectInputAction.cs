namespace Mississippi.Refraction.Components.Molecules.MisSelectActions;

/// <summary>
///     Represents an input interaction action emitted by
///     <see cref="global::Mississippi.Refraction.Components.Molecules.MisSelect" />.
/// </summary>
/// <param name="IntentId">The intent identifier from the select view model.</param>
/// <param name="Value">The current selected value.</param>
public sealed record MisSelectInputAction(
    string IntentId,
    string Value
) : IMisSelectAction;