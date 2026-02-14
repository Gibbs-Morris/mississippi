using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisSelect;

/// <summary>
///     Action dispatched to set the optional select title.
/// </summary>
/// <param name="Title">The title value.</param>
internal sealed record SetMisSelectTitleAction(string? Title) : IAction;