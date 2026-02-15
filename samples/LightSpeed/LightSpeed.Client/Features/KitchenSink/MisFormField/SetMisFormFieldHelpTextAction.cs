using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisFormField;

/// <summary>Action dispatched to set the help text.</summary>
internal sealed record SetMisFormFieldHelpTextAction(string? HelpText) : IAction;