using System;
using System.Collections.Generic;

using Mississippi.Refraction.Components.Molecules.MisButton;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisButton;

/// <summary>
///     Selectors for deriving values from <see cref="MisButtonKitchenSinkState" />.
/// </summary>
internal static class MisButtonKitchenSinkSelectors
{
    /// <summary>
    ///     Gets the current MisButton view model.
    /// </summary>
    /// <param name="state">The Kitchen Sink feature state.</param>
    /// <returns>The current button view model.</returns>
    public static MisButtonViewModel GetViewModel(
        MisButtonKitchenSinkState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.ViewModel;
    }

    /// <summary>
    ///     Gets the current list of logged button events.
    /// </summary>
    /// <param name="state">The Kitchen Sink feature state.</param>
    /// <returns>The event log entries.</returns>
    public static IReadOnlyList<string> GetEventLog(
        MisButtonKitchenSinkState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.EventLog;
    }
}