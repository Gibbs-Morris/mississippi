using Mississippi.Refraction.Components.Molecules;
using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisCheckbox;

/// <summary>
///     Action dispatched to set the checkbox semantic visual state.
/// </summary>
/// <param name="State">The visual state.</param>
internal sealed record SetMisCheckboxStateAction(MisCheckboxState State) : IAction;