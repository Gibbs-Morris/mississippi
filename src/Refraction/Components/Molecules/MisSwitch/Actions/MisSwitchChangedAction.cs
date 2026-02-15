namespace Mississippi.Refraction.Components.Molecules.MisSwitchActions;

/// <summary>
///     Represents a change interaction action emitted by
///     <see cref="global::Mississippi.Refraction.Components.Molecules.MisSwitch" />.
/// </summary>
/// <param name="IntentId">The intent identifier from the switch view model.</param>
/// <param name="IsChecked">A value indicating whether the switch is checked.</param>
public sealed record MisSwitchChangedAction(string IntentId, bool IsChecked) : IMisSwitchAction;