using System;

using Mississippi.Refraction.Components.Molecules;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisLabel;

/// <summary>
///     Selectors for deriving values from <see cref="MisLabelKitchenSinkState" />.
/// </summary>
internal static class MisLabelKitchenSinkSelectors
{
    /// <summary>
    ///     Gets the label text content.
    /// </summary>
    /// <param name="state">The Kitchen Sink feature state.</param>
    /// <returns>The label text.</returns>
    public static string GetLabelText(
        MisLabelKitchenSinkState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.LabelText;
    }

    /// <summary>
    ///     Gets the current MisLabel view model.
    /// </summary>
    /// <param name="state">The Kitchen Sink feature state.</param>
    /// <returns>The current label view model.</returns>
    public static MisLabelViewModel GetViewModel(
        MisLabelKitchenSinkState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.ViewModel;
    }
}