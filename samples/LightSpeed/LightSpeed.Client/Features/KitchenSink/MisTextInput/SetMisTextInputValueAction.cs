using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisTextInput;

/// <summary>
///     Action dispatched to set the current text input value.
/// </summary>
/// <param name="Value">The current input value.</param>
internal sealed record SetMisTextInputValueAction(string Value) : IAction;