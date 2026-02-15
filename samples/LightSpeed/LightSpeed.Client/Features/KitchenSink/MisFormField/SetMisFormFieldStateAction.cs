using Mississippi.Refraction.Components.Molecules;
using Mississippi.Reservoir.Abstractions.Actions;

namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisFormField;

/// <summary>Action dispatched to set the visual state.</summary>
internal sealed record SetMisFormFieldStateAction(MisFormFieldState State) : IAction;
