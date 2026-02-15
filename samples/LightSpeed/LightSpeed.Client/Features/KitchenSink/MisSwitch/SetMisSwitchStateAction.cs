using Mississippi.Refraction.Components.Molecules;
using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisSwitch;

/// <summary>
///     Action dispatched to set the switch semantic visual state.
/// </summary>
/// <param name="State">The visual state.</param>
internal sealed record SetMisSwitchStateAction(MisSwitchState State) : IAction;