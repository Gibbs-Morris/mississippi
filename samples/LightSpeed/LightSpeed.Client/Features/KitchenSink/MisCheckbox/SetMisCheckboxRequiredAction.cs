using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisCheckbox;

/// <summary>
///     Action dispatched to set whether the checkbox is required.
/// </summary>
/// <param name="IsRequired">A value indicating whether the checkbox is required.</param>
internal sealed record SetMisCheckboxRequiredAction(bool IsRequired) : IAction;