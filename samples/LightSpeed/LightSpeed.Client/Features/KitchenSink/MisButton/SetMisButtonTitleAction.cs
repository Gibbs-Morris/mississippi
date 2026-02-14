using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisButton;

/// <summary>
///     Action dispatched to set the button title.
/// </summary>
/// <param name="Title">The title attribute value.</param>
internal sealed record SetMisButtonTitleAction(string? Title) : IAction;
