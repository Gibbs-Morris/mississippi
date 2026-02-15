using Mississippi.Refraction.Components.Molecules;
using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisValidationMessage;

/// <summary>
///     Action dispatched to set the severity of the validation message.
/// </summary>
/// <param name="Severity">The new severity level.</param>
internal sealed record SetMisValidationMessageSeverityAction(MisValidationMessageSeverity Severity) : IAction;