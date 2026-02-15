using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisRadioGroup;

/// <summary>
///     Action dispatched to set the optional radio group aria label.
/// </summary>
/// <param name="AriaLabel">The aria label value.</param>
internal sealed record SetMisRadioGroupAriaLabelAction(string? AriaLabel) : IAction;