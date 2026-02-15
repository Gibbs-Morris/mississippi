using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisCheckboxGroup;

/// <summary>
///     Sets the aria-label on the MisCheckboxGroup demo's view model.
/// </summary>
/// <param name="AriaLabel">The new aria-label to set, or null to clear.</param>
internal sealed record SetMisCheckboxGroupAriaLabelAction(string? AriaLabel) : IAction;