using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisTextarea;

/// <summary>
///     Action dispatched to set the optional textarea CSS class.
/// </summary>
/// <param name="CssClass">The CSS class value.</param>
internal sealed record SetMisTextareaCssClassAction(string? CssClass) : IAction;