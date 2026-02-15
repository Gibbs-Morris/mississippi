using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisCheckboxGroup;

/// <summary>
///     Action to record an event from the checkbox group component.
/// </summary>
/// <param name="EventName">The name of the event.</param>
/// <param name="Details">Event details.</param>
public sealed record RecordMisCheckboxGroupEventAction(string EventName, string Details) : IAction;