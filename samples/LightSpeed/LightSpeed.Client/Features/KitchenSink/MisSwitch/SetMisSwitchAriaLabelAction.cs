using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisSwitch;

/// <summary>
///     Action dispatched to set the optional switch aria label.
/// </summary>
/// <param name="AriaLabel">The aria label value.</param>
internal sealed record SetMisSwitchAriaLabelAction(string? AriaLabel) : IAction;