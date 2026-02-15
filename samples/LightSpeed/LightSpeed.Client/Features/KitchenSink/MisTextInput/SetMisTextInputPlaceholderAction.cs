using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisTextInput;

/// <summary>
///     Action dispatched to set the text input placeholder.
/// </summary>
/// <param name="Placeholder">The placeholder value.</param>
internal sealed record SetMisTextInputPlaceholderAction(string? Placeholder) : IAction;