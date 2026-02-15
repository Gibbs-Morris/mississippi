using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisTextInput;

/// <summary>
///     Action dispatched to enable or disable the text input.
/// </summary>
/// <param name="IsDisabled">A value indicating whether the input should be disabled.</param>
internal sealed record SetMisTextInputDisabledAction(bool IsDisabled) : IAction;