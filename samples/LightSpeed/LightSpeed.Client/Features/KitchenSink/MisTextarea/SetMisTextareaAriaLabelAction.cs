using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisTextarea;

/// <summary>
///     Action dispatched to set the optional textarea aria label.
/// </summary>
/// <param name="AriaLabel">The aria label value.</param>
internal sealed record SetMisTextareaAriaLabelAction(string? AriaLabel) : IAction;
