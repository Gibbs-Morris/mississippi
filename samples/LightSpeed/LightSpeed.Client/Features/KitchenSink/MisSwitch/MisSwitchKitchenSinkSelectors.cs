using System;
using System.Collections.Generic;

using Mississippi.Refraction.Components.Molecules;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisSwitch;

/// <summary>
///     Selectors for deriving values from <see cref="MisSwitchKitchenSinkState" />.
/// </summary>
internal static class MisSwitchKitchenSinkSelectors
{
    /// <summary>
    ///     Gets the current list of logged switch events.
    /// </summary>
    /// <param name="state">The Kitchen Sink feature state.</param>
    /// <returns>The event log entries.</returns>
    public static IReadOnlyList<string> GetEventLog(
        MisSwitchKitchenSinkState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.EventLog;
    }

    /// <summary>
    ///     Gets the current MisSwitch view model.
    /// </summary>
    /// <param name="state">The Kitchen Sink feature state.</param>
    /// <returns>The current switch view model.</returns>
    public static MisSwitchViewModel GetViewModel(
        MisSwitchKitchenSinkState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.ViewModel;
    }
}