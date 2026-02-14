using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisButton;

/// <summary>
///     Action dispatched to enable or disable the button.
/// </summary>
/// <param name="IsDisabled">A value indicating whether the button should be disabled.</param>
internal sealed record SetMisButtonDisabledAction(bool IsDisabled) : IAction;
