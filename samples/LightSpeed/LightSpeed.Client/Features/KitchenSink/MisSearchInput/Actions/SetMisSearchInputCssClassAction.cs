using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisSearchInput;

/// <summary>
///     Sets the CSS class on the MisSearchInput demo's view model.
/// </summary>
/// <param name="CssClass">The new CSS class to set, or null to clear.</param>
internal sealed record SetMisSearchInputCssClassAction(string? CssClass) : IAction;