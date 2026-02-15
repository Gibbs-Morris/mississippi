using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisPasswordInput;

/// <summary>
///     Action to set the password input aria-label.
/// </summary>
/// <param name="AriaLabel">The aria label text.</param>
public sealed record SetMisPasswordInputAriaLabelAction(string? AriaLabel) : IAction;
