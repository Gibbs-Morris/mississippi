using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisValidationMessage;

/// <summary>
///     Action dispatched to set the 'for' attribute value.
/// </summary>
/// <param name="For">The id of the associated form element.</param>
internal sealed record SetMisValidationMessageForAction(string? For) : IAction;