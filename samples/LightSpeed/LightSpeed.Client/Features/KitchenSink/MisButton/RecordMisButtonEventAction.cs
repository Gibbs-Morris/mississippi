using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisButton;

/// <summary>
///     Action dispatched when a button interaction should be logged.
/// </summary>
/// <param name="EventName">The action type name.</param>
/// <param name="EventDetails">Additional event details.</param>
internal sealed record RecordMisButtonEventAction(string EventName, string EventDetails) : IAction;
