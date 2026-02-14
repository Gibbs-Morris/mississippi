using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisSelect;

/// <summary>
///     Action dispatched to set the selected value.
/// </summary>
/// <param name="Value">The selected value.</param>
internal sealed record SetMisSelectValueAction(string Value) : IAction;