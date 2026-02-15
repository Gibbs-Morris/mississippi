using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisCheckboxGroup;

/// <summary>
///     Sets the CSS class on the MisCheckboxGroup demo's view model.
/// </summary>
/// <param name="CssClass">The new CSS class to set, or null to clear.</param>
internal sealed record SetMisCheckboxGroupCssClassAction(string? CssClass) : IAction;