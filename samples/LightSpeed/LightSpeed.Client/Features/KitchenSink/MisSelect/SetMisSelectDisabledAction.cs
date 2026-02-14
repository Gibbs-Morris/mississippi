using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisSelect;

/// <summary>
///     Action dispatched to set whether the select is disabled.
/// </summary>
/// <param name="IsDisabled">A value indicating whether the select is disabled.</param>
internal sealed record SetMisSelectDisabledAction(bool IsDisabled) : IAction;