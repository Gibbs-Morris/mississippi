using System;
using System.Collections.Generic;

using Mississippi.Refraction.Components.Molecules;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisTextInput;

/// <summary>
///     Selectors for deriving values from <see cref="MisTextInputKitchenSinkState" />.
/// </summary>
internal static class MisTextInputKitchenSinkSelectors
{
    /// <summary>
    ///     Gets the current MisTextInput view model.
    /// </summary>
    /// <param name="state">The Kitchen Sink feature state.</param>
    /// <returns>The current text input view model.</returns>
    public static MisTextInputViewModel GetViewModel(
        MisTextInputKitchenSinkState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.ViewModel;
    }

    /// <summary>
    ///     Gets the current list of logged text input events.
    /// </summary>
    /// <param name="state">The Kitchen Sink feature state.</param>
    /// <returns>The event log entries.</returns>
    public static IReadOnlyList<string> GetEventLog(
        MisTextInputKitchenSinkState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.EventLog;
    }
}
