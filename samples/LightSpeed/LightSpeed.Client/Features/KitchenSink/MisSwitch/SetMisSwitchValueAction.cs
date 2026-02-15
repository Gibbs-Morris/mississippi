using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisSwitch;

/// <summary>
///     Action dispatched to set the switch value.
/// </summary>
/// <param name="Value">The switch value.</param>
internal sealed record SetMisSwitchValueAction(string Value) : IAction;