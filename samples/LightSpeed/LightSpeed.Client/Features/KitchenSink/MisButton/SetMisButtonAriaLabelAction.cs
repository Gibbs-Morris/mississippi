using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisButton;

/// <summary>
///     Action dispatched to set the button aria label.
/// </summary>
/// <param name="AriaLabel">The aria label value.</param>
internal sealed record SetMisButtonAriaLabelAction(string? AriaLabel) : IAction;
