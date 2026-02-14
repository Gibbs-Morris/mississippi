using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisSelect;

/// <summary>
///     Action dispatched to set the optional select aria label.
/// </summary>
/// <param name="AriaLabel">The aria label value.</param>
internal sealed record SetMisSelectAriaLabelAction(string? AriaLabel) : IAction;