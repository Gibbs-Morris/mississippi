using System.Collections.Generic;

using Mississippi.Refraction.Components.Molecules;
using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisSelect;

/// <summary>
///     Action dispatched to set select options.
/// </summary>
/// <param name="Options">The options list.</param>
internal sealed record SetMisSelectOptionsAction(IReadOnlyList<MisSelectOptionViewModel> Options) : IAction;