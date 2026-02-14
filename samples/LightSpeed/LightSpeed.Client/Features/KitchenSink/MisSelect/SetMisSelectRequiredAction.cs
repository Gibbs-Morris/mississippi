using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisSelect;

/// <summary>
///     Action dispatched to set whether the select is required.
/// </summary>
/// <param name="IsRequired">A value indicating whether the select is required.</param>
internal sealed record SetMisSelectRequiredAction(bool IsRequired) : IAction;