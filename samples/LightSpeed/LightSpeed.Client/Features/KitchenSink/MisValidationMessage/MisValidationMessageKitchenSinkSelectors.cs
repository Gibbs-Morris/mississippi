using System;

using Mississippi.Refraction.Components.Molecules;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisValidationMessage;

/// <summary>
///     Selectors for deriving values from <see cref="MisValidationMessageKitchenSinkState" />.
/// </summary>
internal static class MisValidationMessageKitchenSinkSelectors
{
    /// <summary>
    ///     Gets the current MisValidationMessage view model.
    /// </summary>
    /// <param name="state">The Kitchen Sink feature state.</param>
    /// <returns>The current validation message view model.</returns>
    public static MisValidationMessageViewModel GetViewModel(
        MisValidationMessageKitchenSinkState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.ViewModel;
    }

    /// <summary>
    ///     Gets the message text content.
    /// </summary>
    /// <param name="state">The Kitchen Sink feature state.</param>
    /// <returns>The message text.</returns>
    public static string GetMessageText(
        MisValidationMessageKitchenSinkState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.MessageText;
    }
}
