using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisSwitch;

/// <summary>
///     Action dispatched to set whether the switch is checked.
/// </summary>
/// <param name="IsChecked">A value indicating whether the switch is checked.</param>
internal sealed record SetMisSwitchCheckedAction(bool IsChecked) : IAction;
