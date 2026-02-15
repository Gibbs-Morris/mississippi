using System.Collections.Generic;

using Mississippi.Refraction.Components.Molecules;
using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisCheckboxGroup;

/// <summary>
///     Sets the options on the MisCheckboxGroup demo's view model.
/// </summary>
/// <param name="Options">The new options to set.</param>
internal sealed record SetMisCheckboxGroupOptionsAction(IReadOnlyList<MisCheckboxOptionViewModel> Options) : IAction;