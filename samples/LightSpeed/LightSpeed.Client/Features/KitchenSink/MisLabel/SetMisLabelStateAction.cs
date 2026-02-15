using Mississippi.Refraction.Components.Molecules;
using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisLabel;

/// <summary>
///     Action dispatched to set the visual state of the label.
/// </summary>
/// <param name="State">The new visual state.</param>
internal sealed record SetMisLabelStateAction(MisLabelState State) : IAction;