using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisTextarea;

/// <summary>
///     Action dispatched to set the optional textarea placeholder.
/// </summary>
/// <param name="Placeholder">The placeholder value.</param>
internal sealed record SetMisTextareaPlaceholderAction(string? Placeholder) : IAction;