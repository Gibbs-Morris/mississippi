using System.Collections.Generic;

using Mississippi.Refraction.Components.Molecules;
using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisRadioGroup;

/// <summary>
///     Action dispatched to set radio options.
/// </summary>
/// <param name="Options">The options list.</param>
internal sealed record SetMisRadioGroupOptionsAction(IReadOnlyList<MisRadioOptionViewModel> Options) : IAction;
