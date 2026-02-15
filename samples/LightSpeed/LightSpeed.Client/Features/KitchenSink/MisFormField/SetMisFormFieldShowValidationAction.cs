using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisFormField;

/// <summary>Action dispatched to set whether to show validation.</summary>
internal sealed record SetMisFormFieldShowValidationAction(bool ShowValidation) : IAction;