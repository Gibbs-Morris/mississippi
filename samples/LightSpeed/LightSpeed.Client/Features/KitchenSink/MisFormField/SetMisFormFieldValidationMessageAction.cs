using Mississippi.Reservoir.Abstractions.Actions;

namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisFormField;

/// <summary>Action dispatched to set the validation message.</summary>
internal sealed record SetMisFormFieldValidationMessageAction(string? ValidationMessage) : IAction;
