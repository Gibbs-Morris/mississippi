using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisTextInput;

/// <summary>
///     Action dispatched to set the autocomplete value.
/// </summary>
/// <param name="AutoComplete">The autocomplete value.</param>
internal sealed record SetMisTextInputAutoCompleteAction(string? AutoComplete) : IAction;