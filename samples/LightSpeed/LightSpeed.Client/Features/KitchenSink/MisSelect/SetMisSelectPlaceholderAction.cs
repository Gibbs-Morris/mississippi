using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisSelect;

/// <summary>
///     Action dispatched to set the optional select placeholder.
/// </summary>
/// <param name="Placeholder">The placeholder value.</param>
internal sealed record SetMisSelectPlaceholderAction(string? Placeholder) : IAction;