using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisSwitch;

/// <summary>
///     Action dispatched to clear logged MisSwitch interaction events.
/// </summary>
internal sealed record ClearMisSwitchEventsAction : IAction;