using System;

using Mississippi.Refraction.Components.Molecules;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisHelpText;

/// <summary>
///     Selectors for deriving values from <see cref="MisHelpTextKitchenSinkState" />.
/// </summary>
internal static class MisHelpTextKitchenSinkSelectors
{
    /// <summary>
    ///     Gets the help text content.
    /// </summary>
    /// <param name="state">The Kitchen Sink feature state.</param>
    /// <returns>The help text content.</returns>
    public static string GetHelpTextContent(
        MisHelpTextKitchenSinkState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.HelpTextContent;
    }

    /// <summary>
    ///     Gets the current MisHelpText view model.
    /// </summary>
    /// <param name="state">The Kitchen Sink feature state.</param>
    /// <returns>The current help text view model.</returns>
    public static MisHelpTextViewModel GetViewModel(
        MisHelpTextKitchenSinkState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.ViewModel;
    }
}