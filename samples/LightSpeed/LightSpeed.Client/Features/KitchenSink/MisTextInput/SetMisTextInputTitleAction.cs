using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisTextInput;

/// <summary>
///     Action dispatched to set the text input title.
/// </summary>
/// <param name="Title">The title value.</param>
internal sealed record SetMisTextInputTitleAction(string? Title) : IAction;