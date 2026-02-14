using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisCheckbox;

/// <summary>
///     Action dispatched to set whether the checkbox is disabled.
/// </summary>
/// <param name="IsDisabled">A value indicating whether the checkbox is disabled.</param>
internal sealed record SetMisCheckboxDisabledAction(bool IsDisabled) : IAction;
