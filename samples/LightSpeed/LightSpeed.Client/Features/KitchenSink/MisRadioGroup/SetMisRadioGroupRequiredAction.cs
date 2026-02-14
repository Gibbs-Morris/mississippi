using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisRadioGroup;

/// <summary>
///     Action dispatched to set whether the radio group is required.
/// </summary>
/// <param name="IsRequired">A value indicating whether the radio group is required.</param>
internal sealed record SetMisRadioGroupRequiredAction(bool IsRequired) : IAction;
