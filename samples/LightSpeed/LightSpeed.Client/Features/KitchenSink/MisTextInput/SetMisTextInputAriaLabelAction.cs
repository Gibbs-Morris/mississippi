using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisTextInput;

/// <summary>
///     Action dispatched to set the text input aria label.
/// </summary>
/// <param name="AriaLabel">The aria label value.</param>
internal sealed record SetMisTextInputAriaLabelAction(string? AriaLabel) : IAction;
