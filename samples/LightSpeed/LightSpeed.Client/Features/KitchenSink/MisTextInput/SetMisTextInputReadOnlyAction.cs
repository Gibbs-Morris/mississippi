using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisTextInput;

/// <summary>
///     Action dispatched to set read-only mode for the text input.
/// </summary>
/// <param name="IsReadOnly">A value indicating whether the input should be read-only.</param>
internal sealed record SetMisTextInputReadOnlyAction(bool IsReadOnly) : IAction;