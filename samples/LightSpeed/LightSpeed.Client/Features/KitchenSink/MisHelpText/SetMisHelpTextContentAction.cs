using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisHelpText;

/// <summary>
///     Action dispatched to set the help text content.
/// </summary>
/// <param name="Content">The new help text content.</param>
internal sealed record SetMisHelpTextContentAction(string? Content) : IAction;
