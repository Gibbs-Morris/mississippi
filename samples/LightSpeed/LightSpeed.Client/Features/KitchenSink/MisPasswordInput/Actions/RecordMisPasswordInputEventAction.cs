using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisPasswordInput;

/// <summary>
///     Action to record an event from the password input component.
/// </summary>
/// <param name="EventName">The name of the event.</param>
/// <param name="Details">Event details.</param>
public sealed record RecordMisPasswordInputEventAction(string EventName, string Details) : IAction;