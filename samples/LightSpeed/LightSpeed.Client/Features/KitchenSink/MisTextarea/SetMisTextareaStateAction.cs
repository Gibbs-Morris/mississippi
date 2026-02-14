using Mississippi.Refraction.Components.Molecules;
using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisTextarea;

/// <summary>
///     Action dispatched to set the textarea semantic visual state.
/// </summary>
/// <param name="State">The visual state.</param>
internal sealed record SetMisTextareaStateAction(MisTextareaState State) : IAction;
