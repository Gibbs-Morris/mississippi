using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisFormField;

/// <summary>Action dispatched to set the disabled state.</summary>
internal sealed record SetMisFormFieldDisabledAction(bool IsDisabled) : IAction;