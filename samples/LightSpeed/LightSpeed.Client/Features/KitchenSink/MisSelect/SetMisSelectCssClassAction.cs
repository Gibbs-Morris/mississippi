using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisSelect;

/// <summary>
///     Action dispatched to set the optional select CSS class.
/// </summary>
/// <param name="CssClass">The CSS class value.</param>
internal sealed record SetMisSelectCssClassAction(string? CssClass) : IAction;