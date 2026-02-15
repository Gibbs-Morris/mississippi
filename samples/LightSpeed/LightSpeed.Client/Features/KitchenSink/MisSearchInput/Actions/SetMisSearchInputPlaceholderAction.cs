using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisSearchInput;

/// <summary>
///     Action to set the search input placeholder text.
/// </summary>
/// <param name="Placeholder">The placeholder text.</param>
public sealed record SetMisSearchInputPlaceholderAction(string? Placeholder) : IAction;