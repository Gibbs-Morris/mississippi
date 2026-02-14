using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisSelect;

/// <summary>
///     Action dispatched to set the select intent identifier.
/// </summary>
/// <param name="IntentId">The intent identifier.</param>
internal sealed record SetMisSelectIntentIdAction(string IntentId) : IAction;