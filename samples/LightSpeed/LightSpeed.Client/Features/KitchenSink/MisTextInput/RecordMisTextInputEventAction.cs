using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisTextInput;

/// <summary>
///     Action dispatched when a text input interaction should be logged.
/// </summary>
/// <param name="EventName">The action type name.</param>
/// <param name="EventDetails">Additional event details.</param>
internal sealed record RecordMisTextInputEventAction(string EventName, string EventDetails) : IAction;