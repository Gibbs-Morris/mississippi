namespace Mississippi.Refraction.Components.Molecules.MisSwitchActions;

/// <summary>
///     Represents a blur interaction action emitted by
///     <see cref="global::Mississippi.Refraction.Components.Molecules.MisSwitch" />.
/// </summary>
/// <param name="IntentId">The intent identifier from the switch view model.</param>
public sealed record MisSwitchBlurredAction(
    string IntentId
) : IMisSwitchAction;
