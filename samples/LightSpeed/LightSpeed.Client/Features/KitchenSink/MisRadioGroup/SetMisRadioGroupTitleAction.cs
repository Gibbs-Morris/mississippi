using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisRadioGroup;

/// <summary>
///     Action dispatched to set the optional radio group title.
/// </summary>
/// <param name="Title">The title value.</param>
internal sealed record SetMisRadioGroupTitleAction(string? Title) : IAction;