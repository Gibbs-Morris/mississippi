using System;
using System.Collections.Generic;

using Mississippi.Refraction.Components.Molecules;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisPasswordInput;

/// <summary>
///     Selectors for deriving values from <see cref="MisPasswordInputKitchenSinkState" />.
/// </summary>
internal static class MisPasswordInputKitchenSinkSelectors
{
    /// <summary>
    ///     Gets the current event log.
    /// </summary>
    /// <param name="state">The feature state.</param>
    /// <returns>The list of logged events.</returns>
    public static IReadOnlyList<string> GetEventLog(
        MisPasswordInputKitchenSinkState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.EventLog;
    }

    /// <summary>
    ///     Gets the current password input view model.
    /// </summary>
    /// <param name="state">The feature state.</param>
    /// <returns>The current view model.</returns>
    public static MisPasswordInputViewModel GetViewModel(
        MisPasswordInputKitchenSinkState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.ViewModel;
    }
}