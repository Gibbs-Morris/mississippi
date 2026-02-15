namespace Mississippi.Refraction.Components.Molecules.MisSelectActions;

/// <summary>
///     Represents a blur interaction action emitted by
///     <see cref="global::Mississippi.Refraction.Components.Molecules.MisSelect" />.
/// </summary>
/// <param name="IntentId">The intent identifier from the select view model.</param>
public sealed record MisSelectBlurredAction(string IntentId) : IMisSelectAction;