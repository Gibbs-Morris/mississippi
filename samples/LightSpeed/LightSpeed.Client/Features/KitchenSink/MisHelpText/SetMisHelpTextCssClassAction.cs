using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisHelpText;

/// <summary>
///     Action dispatched to set the optional CSS class.
/// </summary>
/// <param name="CssClass">The new CSS class.</param>
internal sealed record SetMisHelpTextCssClassAction(string? CssClass) : IAction;