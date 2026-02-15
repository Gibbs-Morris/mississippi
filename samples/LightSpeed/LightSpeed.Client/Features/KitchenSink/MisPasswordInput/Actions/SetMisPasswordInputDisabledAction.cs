using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisPasswordInput;

/// <summary>
///     Action to set the password input disabled state.
/// </summary>
/// <param name="IsDisabled">True to disable the input.</param>
public sealed record SetMisPasswordInputDisabledAction(bool IsDisabled) : IAction;
