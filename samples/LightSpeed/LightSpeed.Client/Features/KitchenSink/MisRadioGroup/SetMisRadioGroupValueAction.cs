using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisRadioGroup;

/// <summary>
///     Action dispatched to set the selected radio value.
/// </summary>
/// <param name="Value">The selected value.</param>
internal sealed record SetMisRadioGroupValueAction(string Value) : IAction;
