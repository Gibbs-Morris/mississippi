using Mississippi.Refraction.Components.Molecules;
using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisButton;

/// <summary>
///     Action dispatched to set the button type.
/// </summary>
/// <param name="Type">The selected button type.</param>
internal sealed record SetMisButtonTypeAction(MisButtonType Type) : IAction;