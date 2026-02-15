using System;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisFormField;

/// <summary>
///     Reducers for the MisFormField Kitchen Sink feature state.
/// </summary>
internal static class MisFormFieldKitchenSinkReducers
{
    /// <summary>
    ///     Sets the label text.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisFormFieldKitchenSinkState SetLabelText(
        MisFormFieldKitchenSinkState state,
        SetMisFormFieldLabelTextAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with { LabelText = action.LabelText ?? "Username" };
    }

    /// <summary>
    ///     Sets the help text.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisFormFieldKitchenSinkState SetHelpText(
        MisFormFieldKitchenSinkState state,
        SetMisFormFieldHelpTextAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with { HelpText = action.HelpText ?? string.Empty };
    }

    /// <summary>
    ///     Sets the validation message.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisFormFieldKitchenSinkState SetValidationMessage(
        MisFormFieldKitchenSinkState state,
        SetMisFormFieldValidationMessageAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with { ValidationMessage = action.ValidationMessage ?? string.Empty };
    }

    /// <summary>
    ///     Sets the input value.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisFormFieldKitchenSinkState SetInputValue(
        MisFormFieldKitchenSinkState state,
        SetMisFormFieldInputValueAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with { InputValue = action.InputValue ?? string.Empty };
    }

    /// <summary>
    ///     Sets whether to show validation.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisFormFieldKitchenSinkState SetShowValidation(
        MisFormFieldKitchenSinkState state,
        SetMisFormFieldShowValidationAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with { ShowValidation = action.ShowValidation };
    }

    /// <summary>
    ///     Sets the visual state.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisFormFieldKitchenSinkState SetState(
        MisFormFieldKitchenSinkState state,
        SetMisFormFieldStateAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with { ViewModel = state.ViewModel with { State = action.State } };
    }

    /// <summary>
    ///     Sets the disabled state.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisFormFieldKitchenSinkState SetDisabled(
        MisFormFieldKitchenSinkState state,
        SetMisFormFieldDisabledAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with { ViewModel = state.ViewModel with { IsDisabled = action.IsDisabled } };
    }

    /// <summary>
    ///     Sets the optional CSS class.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisFormFieldKitchenSinkState SetCssClass(
        MisFormFieldKitchenSinkState state,
        SetMisFormFieldCssClassAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with
        {
            ViewModel = state.ViewModel with
            {
                CssClass = string.IsNullOrWhiteSpace(action.CssClass) ? null : action.CssClass.Trim(),
            },
        };
    }
}
