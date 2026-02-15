using Mississippi.Refraction.Components.Molecules;
using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisPasswordInput;

/// <summary>
///     Action to set the password input semantic state.
/// </summary>
/// <param name="State">The semantic state.</param>
public sealed record SetMisPasswordInputStateAction(MisPasswordInputState State) : IAction;