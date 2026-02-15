using System;
using System.Collections.Generic;

using Mississippi.Refraction.Components.Molecules;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisCheckboxGroup;

/// <summary>
///     Selectors for the MisCheckboxGroup Kitchen Sink feature state.
/// </summary>
internal static class MisCheckboxGroupKitchenSinkSelectors
{
    /// <summary>
    ///     Gets the current checkbox group view model.
    /// </summary>
    /// <param name="state">The feature state.</param>
    /// <returns>The current view model.</returns>
    public static MisCheckboxGroupViewModel GetViewModel(MisCheckboxGroupKitchenSinkState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.ViewModel;
    }

    /// <summary>
    ///     Gets the current event log.
    /// </summary>
    /// <param name="state">The feature state.</param>
    /// <returns>The list of logged events.</returns>
    public static IReadOnlyList<string> GetEventLog(MisCheckboxGroupKitchenSinkState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.EventLog;
    }
}
