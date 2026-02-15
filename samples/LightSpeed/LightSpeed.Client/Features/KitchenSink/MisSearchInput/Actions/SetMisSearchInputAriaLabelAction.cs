using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisSearchInput;

/// <summary>
///     Sets the aria-label on the MisSearchInput demo's view model.
/// </summary>
/// <param name="AriaLabel">The new aria-label to set, or null to clear.</param>
internal sealed record SetMisSearchInputAriaLabelAction(string? AriaLabel) : IAction;
