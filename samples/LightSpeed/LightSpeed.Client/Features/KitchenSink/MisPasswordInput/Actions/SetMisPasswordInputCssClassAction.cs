using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisPasswordInput;

/// <summary>
///     Action to set the password input CSS class.
/// </summary>
/// <param name="CssClass">The additional CSS class.</param>
public sealed record SetMisPasswordInputCssClassAction(string? CssClass) : IAction;
