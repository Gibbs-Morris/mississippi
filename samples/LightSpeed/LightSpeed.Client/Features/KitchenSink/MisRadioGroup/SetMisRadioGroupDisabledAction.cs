using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisRadioGroup;

/// <summary>
///     Action dispatched to set whether the radio group is disabled.
/// </summary>
/// <param name="IsDisabled">A value indicating whether the radio group is disabled.</param>
internal sealed record SetMisRadioGroupDisabledAction(bool IsDisabled) : IAction;
