using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisTextarea;

/// <summary>
///     Action dispatched when a textarea interaction should be logged.
/// </summary>
/// <param name="EventName">The action type name.</param>
/// <param name="EventDetails">Additional event details.</param>
internal sealed record RecordMisTextareaEventAction(string EventName, string EventDetails) : IAction;