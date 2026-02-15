using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisLabel;

/// <summary>
///     Action dispatched to set the 'for' attribute value.
/// </summary>
/// <param name="For">The id of the associated form element.</param>
internal sealed record SetMisLabelForAction(string? For) : IAction;
