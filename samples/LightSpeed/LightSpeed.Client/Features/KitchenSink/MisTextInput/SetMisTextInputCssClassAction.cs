using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisTextInput;

/// <summary>
///     Action dispatched to set the optional CSS class.
/// </summary>
/// <param name="CssClass">The CSS class value.</param>
internal sealed record SetMisTextInputCssClassAction(string? CssClass) : IAction;
