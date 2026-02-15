using System;

using Mississippi.Refraction.Components.Molecules;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisFormField;

/// <summary>
///     Selectors for deriving values from <see cref="MisFormFieldKitchenSinkState" />.
/// </summary>
internal static class MisFormFieldKitchenSinkSelectors
{
    /// <summary>
    ///     Gets the current MisFormField view model.
    /// </summary>
    /// <param name="state">The Kitchen Sink feature state.</param>
    /// <returns>The current form field view model.</returns>
    public static MisFormFieldViewModel GetViewModel(
        MisFormFieldKitchenSinkState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.ViewModel;
    }

    /// <summary>
    ///     Gets the label text.
    /// </summary>
    /// <param name="state">The Kitchen Sink feature state.</param>
    /// <returns>The label text.</returns>
    public static string GetLabelText(
        MisFormFieldKitchenSinkState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.LabelText;
    }

    /// <summary>
    ///     Gets the help text.
    /// </summary>
    /// <param name="state">The Kitchen Sink feature state.</param>
    /// <returns>The help text.</returns>
    public static string GetHelpText(
        MisFormFieldKitchenSinkState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.HelpText;
    }

    /// <summary>
    ///     Gets the validation message.
    /// </summary>
    /// <param name="state">The Kitchen Sink feature state.</param>
    /// <returns>The validation message.</returns>
    public static string GetValidationMessage(
        MisFormFieldKitchenSinkState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.ValidationMessage;
    }

    /// <summary>
    ///     Gets the input value.
    /// </summary>
    /// <param name="state">The Kitchen Sink feature state.</param>
    /// <returns>The input value.</returns>
    public static string GetInputValue(
        MisFormFieldKitchenSinkState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.InputValue;
    }

    /// <summary>
    ///     Gets whether to show validation.
    /// </summary>
    /// <param name="state">The Kitchen Sink feature state.</param>
    /// <returns>True if validation should be shown.</returns>
    public static bool GetShowValidation(
        MisFormFieldKitchenSinkState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.ShowValidation;
    }
}
