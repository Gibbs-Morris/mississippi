using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisTextarea;

/// <summary>
///     Action dispatched to set the textarea value.
/// </summary>
/// <param name="Value">The textarea value.</param>
internal sealed record SetMisTextareaValueAction(string Value) : IAction;