using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisSwitch;

/// <summary>
///     Action dispatched to set whether the switch is required.
/// </summary>
/// <param name="IsRequired">A value indicating whether the switch is required.</param>
internal sealed record SetMisSwitchRequiredAction(bool IsRequired) : IAction;