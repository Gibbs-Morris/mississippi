using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisTextarea;

/// <summary>
///     Action dispatched to set whether the textarea is disabled.
/// </summary>
/// <param name="IsDisabled">A value indicating whether the textarea is disabled.</param>
internal sealed record SetMisTextareaDisabledAction(bool IsDisabled) : IAction;