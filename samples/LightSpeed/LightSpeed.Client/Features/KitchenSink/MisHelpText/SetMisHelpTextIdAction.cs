using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisHelpText;

/// <summary>
///     Action dispatched to set the id attribute.
/// </summary>
/// <param name="Id">The new id value.</param>
internal sealed record SetMisHelpTextIdAction(string? Id) : IAction;
