using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisCheckbox;

/// <summary>
///     Action dispatched to set the optional checkbox CSS class.
/// </summary>
/// <param name="CssClass">The CSS class value.</param>
internal sealed record SetMisCheckboxCssClassAction(string? CssClass) : IAction;