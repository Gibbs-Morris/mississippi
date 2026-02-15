using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisLabel;

/// <summary>
///     Action dispatched to set the label text content.
/// </summary>
/// <param name="Text">The new label text.</param>
internal sealed record SetMisLabelTextAction(string? Text) : IAction;