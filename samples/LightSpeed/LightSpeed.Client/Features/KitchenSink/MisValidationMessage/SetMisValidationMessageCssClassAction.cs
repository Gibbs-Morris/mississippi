using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisValidationMessage;

/// <summary>
///     Action dispatched to set the optional CSS class.
/// </summary>
/// <param name="CssClass">The new CSS class.</param>
internal sealed record SetMisValidationMessageCssClassAction(string? CssClass) : IAction;