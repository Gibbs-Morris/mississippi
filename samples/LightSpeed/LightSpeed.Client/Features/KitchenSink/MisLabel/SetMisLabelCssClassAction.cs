using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisLabel;

/// <summary>
///     Action dispatched to set the optional CSS class.
/// </summary>
/// <param name="CssClass">The new CSS class.</param>
internal sealed record SetMisLabelCssClassAction(string? CssClass) : IAction;