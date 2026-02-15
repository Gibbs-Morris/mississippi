using System;
using System.Collections.Generic;

using Mississippi.Refraction.Components.Molecules;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisSelect;

/// <summary>
///     Selectors for deriving values from <see cref="MisSelectKitchenSinkState" />.
/// </summary>
internal static class MisSelectKitchenSinkSelectors
{
    /// <summary>
    ///     Gets the current list of logged select events.
    /// </summary>
    /// <param name="state">The Kitchen Sink feature state.</param>
    /// <returns>The event log entries.</returns>
    public static IReadOnlyList<string> GetEventLog(
        MisSelectKitchenSinkState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.EventLog;
    }

    /// <summary>
    ///     Gets the current MisSelect view model.
    /// </summary>
    /// <param name="state">The Kitchen Sink feature state.</param>
    /// <returns>The current select view model.</returns>
    public static MisSelectViewModel GetViewModel(
        MisSelectKitchenSinkState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.ViewModel;
    }
}