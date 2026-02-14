using Mississippi.Refraction.Components.Molecules;
using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisTextInput;

/// <summary>
///     Action dispatched to set the text input type.
/// </summary>
/// <param name="Type">The selected input type.</param>
internal sealed record SetMisTextInputTypeAction(MisTextInputType Type) : IAction;
