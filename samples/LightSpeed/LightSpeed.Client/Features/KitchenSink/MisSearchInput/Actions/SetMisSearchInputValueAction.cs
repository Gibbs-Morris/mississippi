using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisSearchInput;

/// <summary>
///     Action to set the search input value.
/// </summary>
/// <param name="Value">The new value.</param>
public sealed record SetMisSearchInputValueAction(string? Value) : IAction;