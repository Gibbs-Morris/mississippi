using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisButton;

/// <summary>
///     Action dispatched to set the displayed text on the Kitchen Sink button.
/// </summary>
/// <param name="Text">The new button text.</param>
internal sealed record SetMisButtonTextAction(string Text) : IAction;