using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisTextarea;

/// <summary>
///     Action dispatched to set the optional textarea title.
/// </summary>
/// <param name="Title">The title value.</param>
internal sealed record SetMisTextareaTitleAction(string? Title) : IAction;