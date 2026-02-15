using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisButton;

/// <summary>
///     Action dispatched to set the emitted intent identifier.
/// </summary>
/// <param name="IntentId">The intent identifier.</param>
internal sealed record SetMisButtonIntentIdAction(string IntentId) : IAction;