using System;
using System.Collections.Generic;

using Mississippi.Refraction.Components.Molecules;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisCheckbox;

/// <summary>
///     Selectors for deriving values from <see cref="MisCheckboxKitchenSinkState" />.
/// </summary>
internal static class MisCheckboxKitchenSinkSelectors
{
    /// <summary>
    ///     Gets the current MisCheckbox view model.
    /// </summary>
    /// <param name="state">The Kitchen Sink feature state.</param>
    /// <returns>The current checkbox view model.</returns>
    public static MisCheckboxViewModel GetViewModel(
        MisCheckboxKitchenSinkState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.ViewModel;
    }

    /// <summary>
    ///     Gets the current list of logged checkbox events.
    /// </summary>
    /// <param name="state">The Kitchen Sink feature state.</param>
    /// <returns>The event log entries.</returns>
    public static IReadOnlyList<string> GetEventLog(
        MisCheckboxKitchenSinkState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.EventLog;
    }
}
