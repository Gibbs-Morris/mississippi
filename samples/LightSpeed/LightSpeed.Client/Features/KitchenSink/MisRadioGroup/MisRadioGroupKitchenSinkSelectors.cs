using System;
using System.Collections.Generic;

using Mississippi.Refraction.Components.Molecules;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisRadioGroup;

/// <summary>
///     Selectors for deriving values from <see cref="MisRadioGroupKitchenSinkState" />.
/// </summary>
internal static class MisRadioGroupKitchenSinkSelectors
{
    /// <summary>
    ///     Gets the current MisRadioGroup view model.
    /// </summary>
    /// <param name="state">The Kitchen Sink feature state.</param>
    /// <returns>The current radio group view model.</returns>
    public static MisRadioGroupViewModel GetViewModel(
        MisRadioGroupKitchenSinkState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.ViewModel;
    }

    /// <summary>
    ///     Gets the current list of logged radio group events.
    /// </summary>
    /// <param name="state">The Kitchen Sink feature state.</param>
    /// <returns>The event log entries.</returns>
    public static IReadOnlyList<string> GetEventLog(
        MisRadioGroupKitchenSinkState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.EventLog;
    }
}
