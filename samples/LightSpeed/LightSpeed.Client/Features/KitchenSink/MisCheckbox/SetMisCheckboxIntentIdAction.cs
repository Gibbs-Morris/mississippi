using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisCheckbox;

/// <summary>
///     Action dispatched to set the checkbox intent identifier.
/// </summary>
/// <param name="IntentId">The intent identifier.</param>
internal sealed record SetMisCheckboxIntentIdAction(string IntentId) : IAction;