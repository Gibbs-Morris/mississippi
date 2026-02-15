using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisCheckbox;

/// <summary>
///     Action dispatched when a checkbox interaction should be logged.
/// </summary>
/// <param name="EventName">The action type name.</param>
/// <param name="EventDetails">Additional event details.</param>
internal sealed record RecordMisCheckboxEventAction(string EventName, string EventDetails) : IAction;