using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisButton;

/// <summary>
///     Action dispatched to set the optional CSS class.
/// </summary>
/// <param name="CssClass">The CSS class value.</param>
internal sealed record SetMisButtonCssClassAction(string? CssClass) : IAction;