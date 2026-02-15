using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisCheckbox;

/// <summary>
///     Action dispatched to set the optional checkbox title.
/// </summary>
/// <param name="Title">The title value.</param>
internal sealed record SetMisCheckboxTitleAction(string? Title) : IAction;