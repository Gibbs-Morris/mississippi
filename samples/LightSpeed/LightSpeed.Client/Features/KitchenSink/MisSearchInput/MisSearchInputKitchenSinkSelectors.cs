using System;
using System.Collections.Generic;

using Mississippi.Refraction.Components.Molecules;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisSearchInput;

/// <summary>
///     Selectors for the MisSearchInput Kitchen Sink feature state.
/// </summary>
internal static class MisSearchInputKitchenSinkSelectors
{
    /// <summary>
    ///     Gets the current event log.
    /// </summary>
    /// <param name="state">The feature state.</param>
    /// <returns>The list of logged events.</returns>
    public static IReadOnlyList<string> GetEventLog(
        MisSearchInputKitchenSinkState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.EventLog;
    }

    /// <summary>
    ///     Gets the current search input view model.
    /// </summary>
    /// <param name="state">The feature state.</param>
    /// <returns>The current view model.</returns>
    public static MisSearchInputViewModel GetViewModel(
        MisSearchInputKitchenSinkState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.ViewModel;
    }
}