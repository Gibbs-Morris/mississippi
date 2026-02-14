namespace Mississippi.Refraction.Components.Molecules.MisSwitchActions;

/// <summary>
///     Represents an input interaction action emitted by
///     <see cref="global::Mississippi.Refraction.Components.Molecules.MisSwitch" />.
/// </summary>
/// <param name="IntentId">The intent identifier from the switch view model.</param>
/// <param name="IsChecked">A value indicating whether the switch is checked.</param>
public sealed record MisSwitchInputAction(
    string IntentId,
    bool IsChecked
) : IMisSwitchAction;
