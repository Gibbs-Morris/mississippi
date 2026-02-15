using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisValidationMessage;

/// <summary>
///     Action dispatched to set the message text content.
/// </summary>
/// <param name="Text">The new message text.</param>
internal sealed record SetMisValidationMessageTextAction(string? Text) : IAction;