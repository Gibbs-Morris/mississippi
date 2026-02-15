using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisLabel;

/// <summary>
///     Action dispatched to set whether the label indicates a required field.
/// </summary>
/// <param name="IsRequired">Whether the field is required.</param>
internal sealed record SetMisLabelIsRequiredAction(bool IsRequired) : IAction;
