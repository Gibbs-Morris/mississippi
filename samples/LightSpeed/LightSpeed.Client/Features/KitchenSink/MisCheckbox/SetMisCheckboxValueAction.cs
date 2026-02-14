using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisCheckbox;

/// <summary>
///     Action dispatched to set the checkbox submitted value.
/// </summary>
/// <param name="Value">The value attribute.</param>
internal sealed record SetMisCheckboxValueAction(string Value) : IAction;
