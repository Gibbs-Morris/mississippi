using Mississippi.Refraction.Components.Molecules;
using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisRadioGroup;

/// <summary>
///     Action dispatched to set the radio group semantic visual state.
/// </summary>
/// <param name="State">The visual state.</param>
internal sealed record SetMisRadioGroupStateAction(MisRadioGroupState State) : IAction;