using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisTextarea;

/// <summary>
///     Action dispatched to set the textarea intent identifier.
/// </summary>
/// <param name="IntentId">The intent identifier.</param>
internal sealed record SetMisTextareaIntentIdAction(string IntentId) : IAction;