using System;
using System.Collections.Generic;

using Mississippi.Refraction.Components.Molecules;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisTextarea;

/// <summary>
///     Selectors for deriving values from <see cref="MisTextareaKitchenSinkState" />.
/// </summary>
internal static class MisTextareaKitchenSinkSelectors
{
    /// <summary>
    ///     Gets the current list of logged textarea events.
    /// </summary>
    /// <param name="state">The Kitchen Sink feature state.</param>
    /// <returns>The event log entries.</returns>
    public static IReadOnlyList<string> GetEventLog(
        MisTextareaKitchenSinkState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.EventLog;
    }

    /// <summary>
    ///     Gets the current MisTextarea view model.
    /// </summary>
    /// <param name="state">The Kitchen Sink feature state.</param>
    /// <returns>The current textarea view model.</returns>
    public static MisTextareaViewModel GetViewModel(
        MisTextareaKitchenSinkState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.ViewModel;
    }
}