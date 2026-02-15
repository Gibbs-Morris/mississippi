using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisCheckboxGroup;

/// <summary>
///     Action to set the checkbox group disabled state.
/// </summary>
/// <param name="IsDisabled">True to disable the checkbox group.</param>
public sealed record SetMisCheckboxGroupDisabledAction(bool IsDisabled) : IAction;
