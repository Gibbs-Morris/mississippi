using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisFormField;

/// <summary>Action dispatched to set the input value.</summary>
internal sealed record SetMisFormFieldInputValueAction(string? InputValue) : IAction;