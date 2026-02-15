using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisSearchInput;

/// <summary>
///     Action to set the search input disabled state.
/// </summary>
/// <param name="IsDisabled">True to disable the input.</param>
public sealed record SetMisSearchInputDisabledAction(bool IsDisabled) : IAction;
