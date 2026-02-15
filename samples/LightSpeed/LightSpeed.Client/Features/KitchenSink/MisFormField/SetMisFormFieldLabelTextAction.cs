using Mississippi.Reservoir.Abstractions.Actions;

namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisFormField;

/// <summary>Action dispatched to set the label text.</summary>
internal sealed record SetMisFormFieldLabelTextAction(string? LabelText) : IAction;
