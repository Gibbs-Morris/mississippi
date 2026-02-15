using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisSwitch;

/// <summary>
///     Action dispatched to set whether the switch is disabled.
/// </summary>
/// <param name="IsDisabled">A value indicating whether the switch is disabled.</param>
internal sealed record SetMisSwitchDisabledAction(bool IsDisabled) : IAction;