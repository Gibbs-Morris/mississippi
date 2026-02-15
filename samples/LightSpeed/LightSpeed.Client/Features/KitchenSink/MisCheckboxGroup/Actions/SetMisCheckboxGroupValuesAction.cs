using System.Collections.Generic;

using Mississippi.Reservoir.Abstractions.Actions;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisCheckboxGroup;

/// <summary>
///     Action to set the checkbox group selected values.
/// </summary>
/// <param name="Values">The selected values.</param>
public sealed record SetMisCheckboxGroupValuesAction(IEnumerable<string>? Values) : IAction;