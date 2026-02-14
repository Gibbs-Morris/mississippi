using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisSelect;

/// <summary>
///     Action dispatched when a select interaction should be logged.
/// </summary>
/// <param name="EventName">The action type name.</param>
/// <param name="EventDetails">Additional event details.</param>
internal sealed record RecordMisSelectEventAction(string EventName, string EventDetails) : IAction;