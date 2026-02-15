using System;


namespace LightSpeed.Client.Features.KitchenSinkFeatures.MisValidationMessage;

/// <summary>
///     Reducers for the MisValidationMessage Kitchen Sink feature state.
/// </summary>
internal static class MisValidationMessageKitchenSinkReducers
{
    /// <summary>
    ///     Sets the optional CSS class.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisValidationMessageKitchenSinkState SetCssClass(
        MisValidationMessageKitchenSinkState state,
        SetMisValidationMessageCssClassAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with
        {
            ViewModel = state.ViewModel with
            {
                CssClass = NormalizeOptional(action.CssClass),
            },
        };
    }

    /// <summary>
    ///     Sets the 'for' attribute value.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisValidationMessageKitchenSinkState SetFor(
        MisValidationMessageKitchenSinkState state,
        SetMisValidationMessageForAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with
        {
            ViewModel = state.ViewModel with
            {
                For = NormalizeOptional(action.For),
            },
        };
    }

    /// <summary>
    ///     Sets the severity of the validation message.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisValidationMessageKitchenSinkState SetSeverity(
        MisValidationMessageKitchenSinkState state,
        SetMisValidationMessageSeverityAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with
        {
            ViewModel = state.ViewModel with
            {
                Severity = action.Severity,
            },
        };
    }

    /// <summary>
    ///     Sets the message text content.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The update action.</param>
    /// <returns>The updated state.</returns>
    public static MisValidationMessageKitchenSinkState SetText(
        MisValidationMessageKitchenSinkState state,
        SetMisValidationMessageTextAction action
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with
        {
            MessageText = action.Text ?? "This field is required.",
        };
    }

    private static string? NormalizeOptional(
        string? value
    ) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}