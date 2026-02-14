using Mississippi.Refraction.Components.Molecules;
using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisSelect;

/// <summary>
///     Action dispatched to set the select semantic visual state.
/// </summary>
/// <param name="State">The visual state.</param>
internal sealed record SetMisSelectStateAction(MisSelectState State) : IAction;